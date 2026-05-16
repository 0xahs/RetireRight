using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RetireRight.Infrastructure.Database;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<RetireRightDbContext>
{
    public RetireRightDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "RetireRight.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables("RETIRERIGHT_")
            .Build();

        var connectionString = config["RetireRight:Database:ConnectionString"];

        var optionsBuilder = new DbContextOptionsBuilder<RetireRightDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new RetireRightDbContext(optionsBuilder.Options);
    }
}
