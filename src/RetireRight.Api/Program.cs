using Microsoft.AspNetCore.Http.Json;
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

        // Retire PsxTerminal — superseded by StockAnalysisDividends (per-stock /dividend/ page).
        // Remove the data_source_configs row so it disappears from the Pipeline UI. Idempotent.
        var psxTermConfig = (await configRepo.GetAllAsync()).FirstOrDefault(c => c.Type == "PsxTerminal");
        if (psxTermConfig is not null)
        {
            db.DataSourceConfigs.Remove(psxTermConfig);
            db.SaveChanges();
            Log.Information("Removed retired PsxTerminal scraper config");
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
    }

    // ── Middleware ─────────────────────────────────────────────────────────────
    app.UseSerilogRequestLogging();
    app.UseCors();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    // ── Endpoints ─────────────────────────────────────────────────────────────
    app.MapStocksEndpoints();
    app.MapSectorsEndpoints();
    app.MapScreenerEndpoints();
    app.MapDividendsEndpoints();
    app.MapMacroEndpoints();
    app.MapPortfolioEndpoints();
    app.MapPipelineEndpoints();
    app.MapScraperConfigEndpoints();
    app.MapExportEndpoints();

    // Health check
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
        .WithTags("Health");

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
