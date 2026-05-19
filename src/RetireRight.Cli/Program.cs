using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RetireRight.Cli.Commands;
using RetireRight.Core.Configuration;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Export.Exporters;
using RetireRight.Infrastructure.Database;
using RetireRight.Infrastructure.Repositories;
using RetireRight.Scrapers;
using RetireRight.Scrapers.AntiBot;
using Serilog;

// Build configuration
// Config loading order: base → environment-specific → env vars
// appsettings.json defaults to localhost. Production overrides via appsettings.Production.json or env vars.
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

// Build DI container
var services = new ServiceCollection();

// Configuration
var rrOptions = config.GetSection(RetireRightOptions.SectionName).Get<RetireRightOptions>()
    ?? new RetireRightOptions();

services.Configure<RetireRightOptions>(config.GetSection(RetireRightOptions.SectionName));

// Logging
services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddSerilog(dispose: true);
});

// Database — JSONB columns use HasConversion() in DbContext for reliable serialization
services.AddDbContext<RetireRightDbContext>(opts => opts.UseNpgsql(rrOptions.Database.ConnectionString));

// Repositories
services.AddScoped<IStockRepository, StockRepository>();
services.AddScoped<IPriceRepository, PriceRepository>();
services.AddScoped<IDividendRepository, DividendRepository>();
services.AddScoped<IMacroRepository, MacroRepository>();
services.AddScoped<IRawResponseRepository, RawResponseRepository>();
services.AddScoped<IFundamentalsRepository, FundamentalsRepository>();
services.AddScoped<IOwnershipRepository, OwnershipRepository>();
services.AddScoped<IInsiderTransactionRepository, InsiderTransactionRepository>();
services.AddScoped<IScrapeRunRepository, ScrapeRunRepository>();
services.AddScoped<IIndexLevelRepository, IndexLevelRepository>();
services.AddScoped<IComputedMetricsRepository, ComputedMetricsRepository>();

// Anti-bot infrastructure
services.AddSingleton<RequestThrottler>();
services.AddSingleton<RetryPolicy>();

// Repositories (ScraperConfigRepository not in extension — register separately)
services.AddScoped<IScraperConfigRepository, ScraperConfigRepository>();

// Scraper factory (DB-driven)
services.AddScoped<ScraperFactory>();

// Metrics Engine
services.AddScoped<RetireRight.MetricsEngine.IMetricCalculator, RetireRight.MetricsEngine.Calculators.DividendSafetyCalculator>();
services.AddScoped<RetireRight.MetricsEngine.IMetricCalculator, RetireRight.MetricsEngine.Calculators.DividendGrowthCalculator>();
services.AddScoped<RetireRight.MetricsEngine.IMetricCalculator, RetireRight.MetricsEngine.Calculators.ValuationCalculator>();
services.AddScoped<RetireRight.MetricsEngine.IMetricCalculator, RetireRight.MetricsEngine.Calculators.FinancialHealthCalculator>();
services.AddScoped<RetireRight.MetricsEngine.IMetricCalculator, RetireRight.MetricsEngine.Calculators.RiskCalculator>();
services.AddScoped<RetireRight.MetricsEngine.IMetricCalculator, RetireRight.MetricsEngine.Calculators.BankingCalculator>();
services.AddScoped<RetireRight.MetricsEngine.IMetricCalculator, RetireRight.MetricsEngine.Calculators.CompositeScoreCalculator>();
services.AddScoped<RetireRight.MetricsEngine.MetricsEngine>();

// Exporters
services.AddSingleton<IExporter, JsonExporter>();
services.AddSingleton<IExporter>(new BriefingExporter(rrOptions.Export.BriefingMaxLines));

var serviceProvider = services.BuildServiceProvider();

// Seed data_source_configs from appsettings if DB table is empty
using (var scope = serviceProvider.CreateScope())
{
    var configRepo = scope.ServiceProvider.GetRequiredService<IScraperConfigRepository>();
    if (await configRepo.CountAsync() == 0 && rrOptions.DataSources.Count > 0)
    {
        foreach (var ds in rrOptions.DataSources)
            await configRepo.UpsertAsync(DataSourceConfig.FromOptions(ds));
        Log.Information("Seeded {Count} scraper configs from appsettings", rrOptions.DataSources.Count);
    }
}

// Build CLI
var rootCommand = new RootCommand { Description = "RetireRight — PSX data pipeline for dividend investors" };
rootCommand.Subcommands.Add(RunCommand.Create(serviceProvider));
rootCommand.Subcommands.Add(ComputeCommand.Create(serviceProvider));
rootCommand.Subcommands.Add(ExportCommand.Create(serviceProvider));
rootCommand.Subcommands.Add(StatusCommand.Create(serviceProvider));

// Run
try
{
    return await rootCommand.Parse(args).InvokeAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "RetireRight terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
