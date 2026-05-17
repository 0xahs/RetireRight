using Microsoft.AspNetCore.Http.Json;
using RetireRight.Api.Endpoints;
using RetireRight.Api.Jobs;
using RetireRight.Api.Services;
using RetireRight.Core.Configuration;
using RetireRight.Core.Interfaces;
using RetireRight.Export.Exporters;
using RetireRight.Infrastructure.Extensions;
using RetireRight.Scrapers.AntiBot;
using RetireRight.Scrapers.Sources;
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

    // ── Scrapers ───────────────────────────────────────────────────────────────
    foreach (var sourceConfig in rrOptions.DataSources.Where(ds => ds.Enabled))
    {
        switch (sourceConfig.Type.ToLowerInvariant())
        {
            case "sarmaaya":
                builder.Services.AddTransient<IScraper>(sp => new SarmaayaScraper(
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<SarmaayaScraper>(),
                    sp.GetRequiredService<ILoggerFactory>(),
                    sp.GetRequiredService<RequestThrottler>(),
                    sp.GetRequiredService<RetryPolicy>(),
                    sourceConfig,
                    sp.GetRequiredService<IStockRepository>(),
                    sp.GetRequiredService<IPriceRepository>(),
                    sp.GetRequiredService<IDividendRepository>(),
                    sp.GetRequiredService<IRawResponseRepository>(),
                    sp.GetRequiredService<IFundamentalsRepository>(),
                    sp.GetRequiredService<IOwnershipRepository>(),
                    sp.GetRequiredService<IInsiderTransactionRepository>(),
                    sp.GetRequiredService<IIndexLevelRepository>()));
                break;

            case "stockanalysis":
                builder.Services.AddTransient<IScraper>(sp => new StockAnalysisScraper(
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<StockAnalysisScraper>(),
                    sp.GetRequiredService<RequestThrottler>(),
                    sp.GetRequiredService<RetryPolicy>(),
                    sourceConfig,
                    sp.GetRequiredService<IStockRepository>(),
                    sp.GetRequiredService<IFundamentalsRepository>()));
                break;

            case "macro":
                builder.Services.AddTransient<IScraper>(sp => new MacroScraper(
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<MacroScraper>(),
                    sp.GetRequiredService<RequestThrottler>(),
                    sp.GetRequiredService<RetryPolicy>(),
                    sourceConfig,
                    sp.GetRequiredService<IMacroRepository>()));
                break;
        }
    }

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

    // ── Startup: mark orphaned scrape runs as failed ───────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<RetireRight.Infrastructure.Database.RetireRightDbContext>();
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
    app.MapScreenerEndpoints();
    app.MapDividendsEndpoints();
    app.MapMacroEndpoints();
    app.MapPortfolioEndpoints();
    app.MapPipelineEndpoints();
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
