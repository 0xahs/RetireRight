using RetireRight.Core.Configuration;

namespace RetireRight.Core.Models;

public class DataSourceConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public bool Enabled { get; set; } = true;

    public int MaxRequestsPerMinute { get; set; } = 10;
    public int MinDelayMs { get; set; } = 2000;
    public int MaxDelayMs { get; set; } = 5000;

    public int RetryMaxAttempts { get; set; } = 3;
    public int RetryInitialDelayMs { get; set; } = 1000;
    public double RetryBackoffMultiplier { get; set; } = 2.0;

    public string? CredentialEmail { get; set; }
    public string? CredentialPassword { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public DataSourceOptions ToOptions() => new()
    {
        Name = Name,
        Type = Type,
        Description = Description,
        BaseUrl = BaseUrl,
        Enabled = Enabled,
        RateLimit = new RateLimitOptions
        {
            MaxRequestsPerMinute = MaxRequestsPerMinute,
            MinDelayMs = MinDelayMs,
            MaxDelayMs = MaxDelayMs
        },
        Retry = new RetryOptions
        {
            MaxAttempts = RetryMaxAttempts,
            InitialDelayMs = RetryInitialDelayMs,
            BackoffMultiplier = RetryBackoffMultiplier
        },
        Credentials = new CredentialsOptions
        {
            Email = CredentialEmail,
            Password = CredentialPassword
        }
    };

    public static DataSourceConfig FromOptions(DataSourceOptions opts) => new()
    {
        Name = opts.Name,
        Type = opts.Type,
        Description = opts.Description,
        BaseUrl = opts.BaseUrl,
        Enabled = opts.Enabled,
        MaxRequestsPerMinute = opts.RateLimit.MaxRequestsPerMinute,
        MinDelayMs = opts.RateLimit.MinDelayMs,
        MaxDelayMs = opts.RateLimit.MaxDelayMs,
        RetryMaxAttempts = opts.Retry.MaxAttempts,
        RetryInitialDelayMs = opts.Retry.InitialDelayMs,
        RetryBackoffMultiplier = opts.Retry.BackoffMultiplier,
        CredentialEmail = opts.Credentials.Email,
        CredentialPassword = opts.Credentials.Password
    };
}
