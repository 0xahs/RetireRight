using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RetireRight.Api.Endpoints;
using RetireRight.Api.Jobs;
using RetireRight.Api.Services;
using RetireRight.Core.Configuration;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Export.Exporters;
using RetireRight.Infrastructure.Extensions;
using RetireRight.Scrapers;
using RetireRight.Scrapers.AntiBot;
using RetireRight.Screening;
using RetireRight.Screening.Screens;
using Serilog;

// Build configuration
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddJsonFile("appsettings.local.json", optional: true)
    .AddEnvironmentVariables("RETIRERIGHT_")
    .Build();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: Path.Combine(AppContext.BaseDirectory, "logs", "retireright-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // Merge our config builder's sources (including appsettings.local.json) into the host config
    builder.Configuration.AddConfiguration(config);

    var rrOptions = config.GetSection(RetireRightOptions.SectionName).Get<RetireRightOptions>()
        ?? new RetireRightOptions();

    // ── Infrastructure (DB + all repositories) ────────────────────────────────
    builder.Services.AddRetireRightInfrastructure(builder.Configuration);

    // ── Anti-bot infrastructure (shared by all scrapers) ─────────────────────
    builder.Services.AddSingleton<RequestThrottler>();
    builder.Services.AddSingleton<RetryPolicy>();

    // ── Scraper factory (DB-driven, replaces hardcoded IScraper registrations) ─
    builder.Services.AddScoped<ScraperFactory>();

    // ── Metrics Engine ─────────────────────────────────────────────────────────
    builder.Services.AddScoped<RetireRight.MetricsEngine.IMetricCalculator,
        RetireRight.MetricsEngine.Calculators.DividendSafetyCalculator>();
    builder.Services.AddScoped<RetireRight.MetricsEngine.IMetricCalculator,
        RetireRight.MetricsEngine.Calculators.DividendGrowthCalculator>();
    builder.Services.AddScoped<RetireRight.MetricsEngine.IMetricCalculator,
        RetireRight.MetricsEngine.Calculators.ValuationCalculator>();
    builder.Services.AddScoped<RetireRight.MetricsEngine.IMetricCalculator,
        RetireRight.MetricsEngine.Calculators.FinancialHealthCalculator>();
    builder.Services.AddScoped<RetireRight.MetricsEngine.IMetricCalculator,
        RetireRight.MetricsEngine.Calculators.GrowthQualityCalculator>();
    builder.Services.AddScoped<RetireRight.MetricsEngine.IMetricCalculator,
        RetireRight.MetricsEngine.Calculators.RiskCalculator>();
    builder.Services.AddScoped<RetireRight.MetricsEngine.IMetricCalculator,
        RetireRight.MetricsEngine.Calculators.BankingCalculator>();
    builder.Services.AddScoped<RetireRight.MetricsEngine.IMetricCalculator,
        RetireRight.MetricsEngine.Calculators.CompositeScoreCalculator>();
    builder.Services.AddScoped<RetireRight.MetricsEngine.MetricsEngine>();

    // ── Screening ─────────────────────────────────────────────────────────────
    builder.Services.AddScoped<IScreenDataProvider, ScreenDataProvider>();
    builder.Services.AddScoped<IScreener, StockScreener>();

    // ── Exporters ─────────────────────────────────────────────────────────────
    builder.Services.AddSingleton<IExporter, JsonExporter>();
    builder.Services.AddSingleton<IExporter>(_ => new BriefingExporter(rrOptions.Export.BriefingMaxLines));

    // ── Pipeline services ──────────────────────────────────────────────────────
    builder.Services.AddSingleton<PipelineEventBroadcaster>();
    builder.Services.AddSingleton<PipelineJobService>();
    builder.Services.AddHostedService(sp => sp.GetRequiredService<PipelineJobService>());
    builder.Services.AddHostedService<SchedulerService>();
    builder.Services.AddScoped<PipelineOrchestrator>();

    // ── CORS ───────────────────────────────────────────────────────────────────
    var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:3000"];
    builder.Services.AddCors(opts =>
        opts.AddDefaultPolicy(p => p
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

    // ── Authentication (JWT bearer) ───────────────────────────────────────────
    // JWT signing key MUST come from appsettings.local.json or env var; never committed.
    var signingKey = rrOptions.Auth.JwtSigningKey;
    if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
    {
        throw new InvalidOperationException(
            "RetireRight:Auth:JwtSigningKey must be set and at least 32 characters. " +
            "Set it in appsettings.local.json or env var RETIRERIGHT_RetireRight__Auth__JwtSigningKey.");
    }

    builder.Services.AddSingleton<JwtTokenService>();
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
    builder.Services.AddScoped<RetireRight.Api.Services.ICurrentUserAccessor, RetireRight.Api.Services.CurrentUserAccessor>();
    builder.Services.AddHttpContextAccessor();

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = rrOptions.Auth.Issuer,
                ValidAudience = rrOptions.Auth.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ClockSkew = TimeSpan.FromSeconds(30),
            };
        });

    // Fallback policy: every endpoint requires auth unless it explicitly opts out
    // with [AllowAnonymous] / .AllowAnonymous(). /api/auth/login, /health, and the
    // OpenAPI document are the only anonymous endpoints.
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        options.AddPolicy("Admin", policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("is_admin", "true"));
    });

    // ── Rate limiting (bruteforce / abuse protection) ─────────────────────────
    builder.Services.AddRateLimiter(opts =>
    {
        // login + change-password
        opts.AddFixedWindowLimiter("login", o =>
        {
            o.PermitLimit = 5;
            o.Window = TimeSpan.FromMinutes(5);
            o.QueueLimit = 0;
        });
        // signup — per-IP, much tighter (3/hour) to discourage abuse of email sending
        opts.AddPolicy("signup", httpContext =>
            System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
                factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = 3,
                    Window = TimeSpan.FromHours(1),
                    QueueLimit = 0,
                }));
        opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // ── OpenAPI (dev only) ─────────────────────────────────────────────────────
    builder.Services.AddOpenApi();

    // Configure JSON to handle DateOnly, TimeOnly, decimal precision
    builder.Services.Configure<JsonOptions>(opts =>
    {
        opts.SerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

    var app = builder.Build();

    // ── Startup: seed DB config + mark orphaned runs as failed ───────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<RetireRight.Infrastructure.Database.RetireRightDbContext>();

        // Seed data_source_configs from appsettings if table is empty
        var configRepo = scope.ServiceProvider.GetRequiredService<IScraperConfigRepository>();
        if (await configRepo.CountAsync() == 0 && rrOptions.DataSources.Count > 0)
        {
            foreach (var ds in rrOptions.DataSources)
                await configRepo.UpsertAsync(DataSourceConfig.FromOptions(ds));
            Log.Information("Seeded {Count} scraper configs from appsettings", rrOptions.DataSources.Count);
        }

        // Ensure the Listings scraper config exists (added after the initial seed).
        // The Type identifier is pulled from the scraper class's [DataSource] attribute so it stays the single source of truth.
        var listingsType = typeof(RetireRight.Scrapers.Sources.PsxUniverseScraper)
            .GetCustomAttributes(typeof(RetireRight.Core.Attributes.DataSourceAttribute), false)
            .Cast<RetireRight.Core.Attributes.DataSourceAttribute>()
            .First().Identifier;
        var hasListings = (await configRepo.GetAllAsync()).Any(c => c.Type == listingsType);
        if (!hasListings)
        {
            await configRepo.UpsertAsync(new DataSourceConfig
            {
                Name = "Listings",
                Type = listingsType,
                Description = "PSX universe sync (stockanalysis.com)",
                BaseUrl = "https://stockanalysis.com/list/pakistan-stock-exchange/",
                Enabled = true,
                MaxRequestsPerMinute = 2,
                MinDelayMs = 5000,
                MaxDelayMs = 8000,
                RetryMaxAttempts = 2,
                RetryInitialDelayMs = 3000,
                RetryBackoffMultiplier = 2.0,
            });
            Log.Information("Seeded Listings scraper config (type={Type})", listingsType);
        }

        // Per-stock dividend history (multi-year, Ex/Cash/Record/Pay) from stockanalysis.com.
        // Defaults to watchlist; ~30s/stock so a full-universe run is impractical.
        var divHistType = typeof(RetireRight.Scrapers.Sources.StockAnalysisDividendsScraper)
            .GetCustomAttributes(typeof(RetireRight.Core.Attributes.DataSourceAttribute), false)
            .Cast<RetireRight.Core.Attributes.DataSourceAttribute>()
            .First().Identifier;
        var hasDivHist = (await configRepo.GetAllAsync()).Any(c => c.Type == divHistType);
        if (!hasDivHist)
        {
            await configRepo.UpsertAsync(new DataSourceConfig
            {
                Name = "Dividends History",
                Type = divHistType,
                Description = "Per-stock dividend history (stockanalysis.com)",
                BaseUrl = "https://stockanalysis.com/quote/psx/",
                Enabled = true,
                MaxRequestsPerMinute = 6,
                MinDelayMs = 2000,
                MaxDelayMs = 4000,
                RetryMaxAttempts = 2,
                RetryInitialDelayMs = 3000,
                RetryBackoffMultiplier = 2.0,
            });
            Log.Information("Seeded Dividends History scraper config (type={Type})", divHistType);
        }

        // PsxTerminal re-enabled as a cross-validation dividend source. Ensure the
        // data_source_configs row exists so it appears in the Pipeline UI. Idempotent.
        var psxTermType = typeof(RetireRight.Scrapers.Sources.PsxTerminalScraper)
            .GetCustomAttributes(typeof(RetireRight.Core.Attributes.DataSourceAttribute), false)
            .Cast<RetireRight.Core.Attributes.DataSourceAttribute>()
            .First().Identifier;
        var hasPsxTerm = (await configRepo.GetAllAsync()).Any(c => c.Type == psxTermType);
        if (!hasPsxTerm)
        {
            await configRepo.UpsertAsync(new DataSourceConfig
            {
                Name = "Dividends",
                Type = psxTermType,
                Description = "Per-stock dividend history + homepage upcoming announcements (psxterminal.com)",
                BaseUrl = "https://psxterminal.com",
                Enabled = true,
                MaxRequestsPerMinute = 10,
                MinDelayMs = 1500,
                MaxDelayMs = 3000,
                RetryMaxAttempts = 2,
                RetryInitialDelayMs = 2000,
                RetryBackoffMultiplier = 2.0,
            });
            Log.Information("Seeded PsxTerminal (Dividends) scraper config");
        }

        // One-time cleanup: forward-dated calendar rows seeded by the universe scraper had
        // wrong DPS (the universe "Div. ($)" column is annual trailing total, not per-event).
        // Delete them so the calendar is clean; per-stock StockAnalysisDividends will refill
        // when run for the relevant symbols.
        var todayOnly = DateOnly.FromDateTime(DateTime.UtcNow);
        var staleSeeds = db.Dividends
            .Where(d => d.Source == "StockAnalysisList" && d.ExDate >= todayOnly)
            .ToList();
        if (staleSeeds.Count > 0)
        {
            db.Dividends.RemoveRange(staleSeeds);
            db.SaveChanges();
            Log.Information("Removed {Count} stale universe-seeded calendar rows", staleSeeds.Count);
        }

        var orphaned = db.ScrapeRuns.Where(r => r.Status == "running").ToList();
        foreach (var run in orphaned)
        {
            run.Status = "failed";
            run.CompletedAt ??= DateTime.UtcNow;
        }
        if (orphaned.Count > 0)
        {
            db.SaveChanges();
            Log.Warning("Marked {Count} orphaned scrape run(s) as failed on startup", orphaned.Count);
        }

        // Seed the first user from InitialUser config if users table is empty.
        // Idempotent: once a user exists, this block does nothing — the InitialUser block
        // in appsettings.local.json can be removed after first boot.
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        if (!await userRepo.AnyAsync())
        {
            var initial = rrOptions.InitialUser;
            if (!string.IsNullOrWhiteSpace(initial.Email) && !string.IsNullOrWhiteSpace(initial.Password))
            {
                var hash = BCrypt.Net.BCrypt.HashPassword(initial.Password, workFactor: 12);
                await userRepo.CreateAsync(new User
                {
                    Email = initial.Email.ToLowerInvariant(),
                    PasswordHash = hash,
                    IsAdmin = true,
                    EmailVerifiedAt = DateTime.UtcNow,
                });
                Log.Warning("Seeded initial admin user {Email} (verified) from RetireRight:InitialUser. " +
                            "Remove this block from appsettings.local.json once login is verified.",
                            initial.Email);
            }
            else
            {
                Log.Warning("Users table is empty and no RetireRight:InitialUser config provided. " +
                            "Login will fail until a user is seeded. Set Email/Password under " +
                            "RetireRight:InitialUser in appsettings.local.json and restart.");
            }
        }

        // Multi-user migration backfill — idempotent. Runs once per fresh boot to take
        // pre-multi-user data and assign it to the first admin so it's not stranded.
        var firstAdmin = await userRepo.GetFirstAdminAsync();
        if (firstAdmin is not null)
        {
            // Promote the first user to admin retroactively if no admin exists yet but
            // there's exactly one user (legacy single-user data).
            if (!firstAdmin.IsAdmin)
            {
                firstAdmin.IsAdmin = true;
                firstAdmin.EmailVerifiedAt ??= DateTime.UtcNow;
                await userRepo.UpdateAsync(firstAdmin);
                Log.Warning("Promoted user {UserId} to admin (legacy single-user backfill)", firstAdmin.Id);
            }

            // Backfill portfolio rows that predate the user_id column.
            var orphanHoldings = await db.PortfolioHoldings.Where(h => h.UserId == null).ToListAsync();
            foreach (var h in orphanHoldings) h.UserId = firstAdmin.Id;
            var orphanTx = await db.PortfolioTransactions.Where(t => t.UserId == null).ToListAsync();
            foreach (var t in orphanTx) t.UserId = firstAdmin.Id;
            if (orphanHoldings.Count > 0 || orphanTx.Count > 0)
            {
                await db.SaveChangesAsync();
                Log.Warning("Backfilled user_id on {Holdings} holdings + {Tx} transactions to admin user {UserId}",
                    orphanHoldings.Count, orphanTx.Count, firstAdmin.Id);
            }

            // Seed the admin's personal watchlist from the operational watchlist on first
            // boot only — once seeded, the user manages their watchlist via the UI.
            var adminHasWatchlist = await db.UserWatchlists.AnyAsync(w => w.UserId == firstAdmin.Id);
            if (!adminHasWatchlist)
            {
                var operationalIds = await db.Stocks.Where(s => s.IsWatchlist && s.IsActive)
                    .Select(s => s.Id).ToListAsync();
                if (operationalIds.Count > 0)
                {
                    db.UserWatchlists.AddRange(operationalIds.Select(id =>
                        new RetireRight.Core.Models.UserWatchlist { UserId = firstAdmin.Id, StockId = id }));
                    await db.SaveChangesAsync();
                    Log.Warning("Seeded admin user {UserId} personal watchlist with {Count} stocks from operational",
                        firstAdmin.Id, operationalIds.Count);
                }
            }
        }
    }

    // ── Middleware ─────────────────────────────────────────────────────────────
    app.UseSerilogRequestLogging();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi().AllowAnonymous();
    }

    // ── Endpoints ─────────────────────────────────────────────────────────────
    app.MapAuthEndpoints();
    app.MapStocksEndpoints();
    app.MapSectorsEndpoints();
    app.MapScreenerEndpoints();
    app.MapDividendsEndpoints();
    app.MapMacroEndpoints();
    app.MapPortfolioEndpoints();
    app.MapPipelineEndpoints();
    app.MapScraperConfigEndpoints();
    app.MapExportEndpoints();

    // Health check (anonymous — used by external monitors)
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
        .WithTags("Health")
        .AllowAnonymous();

    Log.Information("RetireRight API starting on {Urls}", string.Join(", ", builder.Configuration["Urls"] ?? "http://localhost:5000"));
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "RetireRight API terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;
