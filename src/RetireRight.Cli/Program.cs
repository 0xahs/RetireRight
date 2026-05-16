using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RetireRight.Cli.Commands;
using RetireRight.Core.Configuration;
using RetireRight.Core.Interfaces;
using RetireRight.Export.Exporters;
using RetireRight.Infrastructure.Database;
using RetireRight.Infrastructure.Repositories;
using RetireRight.Scrapers.AntiBot;
using RetireRight.Scrapers.Sources;
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

// Scrapers — register each source with its DataSourceOptions
foreach (var sourceConfig in rrOptions.DataSources.Where(ds => ds.Enabled))
{
    switch (sourceConfig.Type.ToLowerInvariant())
    {
        case "sarmaaya":
            services.AddTransient<IScraper>(sp => new SarmaayaScraper(
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
            services.AddTransient<IScraper>(sp => new StockAnalysisScraper(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<StockAnalysisScraper>(),
                sp.GetRequiredService<RequestThrottler>(),
                sp.GetRequiredService<RetryPolicy>(),
                sourceConfig,
                sp.GetRequiredService<IStockRepository>(),
                sp.GetRequiredService<IFundamentalsRepository>()));
            break;
    }
}

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
