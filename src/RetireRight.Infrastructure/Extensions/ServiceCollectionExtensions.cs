using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RetireRight.Core.Configuration;
using RetireRight.Core.Interfaces;
using RetireRight.Infrastructure.Database;
using RetireRight.Infrastructure.Repositories;

namespace RetireRight.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers DbContext and all repository implementations.
    /// Scrapers, MetricsEngine, and Screening are registered in the API's Program.cs.
    /// </summary>
    public static IServiceCollection AddRetireRightInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rrOptions = configuration.GetSection(RetireRightOptions.SectionName).Get<RetireRightOptions>()
            ?? new RetireRightOptions();

        services.Configure<RetireRightOptions>(configuration.GetSection(RetireRightOptions.SectionName));

        services.AddDbContext<RetireRightDbContext>(opts =>
            opts.UseNpgsql(rrOptions.Database.ConnectionString));

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
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();

        return services;
    }
}
