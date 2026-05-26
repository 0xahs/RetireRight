namespace RetireRight.Core.Configuration;

public class RetireRightOptions
{
    public const string SectionName = "RetireRight";

    public DatabaseOptions Database { get; set; } = new();
    public ExportOptions Export { get; set; } = new();
    public WatchlistOptions Watchlist { get; set; } = new();
    public List<DataSourceOptions> DataSources { get; set; } = [];
    public PortfolioOptions Portfolio { get; set; } = new();
    public SchedulerOptions Scheduler { get; set; } = new();
    public AuthOptions Auth { get; set; } = new();
    public InitialUserOptions InitialUser { get; set; } = new();
    public EmailOptions Email { get; set; } = new();
}

public class InitialUserOptions
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class DatabaseOptions
{
    public string ConnectionString { get; set; } = "";
}

public class ExportOptions
{
    public string OutputPath { get; set; } = @"C:\Users\ahmad\OneDrive\Documents\Investement\exports";
    public bool GenerateJson { get; set; } = true;
    public bool GenerateBriefing { get; set; } = true;
    public int BriefingMaxLines { get; set; } = 100;
}

public class WatchlistOptions
{
    public List<string> Symbols { get; set; } = [];
}

public class DataSourceOptions
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public RateLimitOptions RateLimit { get; set; } = new();
    public RetryOptions Retry { get; set; } = new();
    public ProxyOptions Proxy { get; set; } = new();
    public CredentialsOptions Credentials { get; set; } = new();
}

public class CredentialsOptions
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class RateLimitOptions
{
    public int MaxRequestsPerMinute { get; set; } = 10;
    public int MinDelayMs { get; set; } = 2000;
    public int MaxDelayMs { get; set; } = 5000;
}

public class RetryOptions
{
    public int MaxAttempts { get; set; } = 3;
    public int InitialDelayMs { get; set; } = 1000;
    public double BackoffMultiplier { get; set; } = 2.0;
}

public class ProxyOptions
{
    public bool Enabled { get; set; }
    public List<string> Urls { get; set; } = [];
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class PortfolioOptions
{
    public string TaxStatus { get; set; } = "Filer";
    public decimal DividendWhtRate { get; set; } = 0.15m;
    public decimal MonthlyInvestment { get; set; } = 15000m;
    public int RetirementHorizonYears { get; set; } = 15;
}

public class SchedulerOptions
{
    public string DailyRunTime { get; set; } = "06:00";
    public bool Enabled { get; set; } = false;
    public List<string> Jobs { get; set; } = ["scrape", "compute", "export"];
}
