namespace RetireRight.Core.Configuration;

public class AuthOptions
{
    public string JwtSigningKey { get; set; } = "";
    public string Issuer { get; set; } = "RetireRight";
    public string Audience { get; set; } = "RetireRight.Web";
    public int TokenLifetimeDays { get; set; } = 30;
    public int LockoutMaxAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 15;
    /// <summary>
    /// Public-facing web app origin (e.g. https://retireright.app). Used to build
    /// the verify-email link sent in signup emails. Required when Email:Enabled = true.
    /// </summary>
    public string AppBaseUrl { get; set; } = "http://localhost:3000";
    /// <summary>If false, /api/auth/signup returns 403 — gates public registration.</summary>
    public bool AllowPublicSignup { get; set; } = true;
    public int EmailVerificationTtlHours { get; set; } = 24;
}
