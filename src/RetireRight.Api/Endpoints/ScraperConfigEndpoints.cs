using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Scrapers;

namespace RetireRight.Api.Endpoints;

public static class ScraperConfigEndpoints
{
    public static void MapScraperConfigEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scrapers").WithTags("Scrapers");

        group.MapGet("/", GetAllAsync).WithName("GetScrapers");
        group.MapPatch("/{name}", PatchAsync).WithName("PatchScraper");
        group.MapPost("/{name}/toggle", ToggleAsync).WithName("ToggleScraper");
        group.MapPut("/{name}/credentials", SetCredentialsAsync).WithName("SetScraperCredentials");
    }

    private static async Task<IResult> GetAllAsync(IScraperConfigRepository repo, CancellationToken ct)
    {
        var configs = await repo.GetAllAsync(ct);
        // Match the execution + pipeline-status display order via the [DataSource(Order=)] attribute.
        return Results.Ok(configs
            .OrderBy(c => ScraperFactory.OrderForType(c.Type))
            .ThenBy(c => c.Id)
            .Select(ToDto));
    }

    private static async Task<IResult> PatchAsync(
        string name,
        ScraperConfigPatchRequest req,
        IScraperConfigRepository repo,
        CancellationToken ct)
    {
        var config = await repo.GetByNameAsync(name, ct);
        if (config is null) return Results.NotFound();

        if (req.Enabled.HasValue) config.Enabled = req.Enabled.Value;
        if (req.Description is not null) config.Description = req.Description;
        if (req.BaseUrl is not null) config.BaseUrl = req.BaseUrl;
        if (req.MaxRequestsPerMinute.HasValue) config.MaxRequestsPerMinute = req.MaxRequestsPerMinute.Value;
        if (req.MinDelayMs.HasValue) config.MinDelayMs = req.MinDelayMs.Value;
        if (req.MaxDelayMs.HasValue) config.MaxDelayMs = req.MaxDelayMs.Value;
        if (req.RetryMaxAttempts.HasValue) config.RetryMaxAttempts = req.RetryMaxAttempts.Value;

        var updated = await repo.UpsertAsync(config, ct);
        return Results.Ok(ToDto(updated));
    }

    private static async Task<IResult> ToggleAsync(
        string name,
        IScraperConfigRepository repo,
        CancellationToken ct)
    {
        var config = await repo.GetByNameAsync(name, ct);
        if (config is null) return Results.NotFound();

        await repo.SetEnabledAsync(name, !config.Enabled, ct);
        config = await repo.GetByNameAsync(name, ct);
        return Results.Ok(ToDto(config!));
    }

    private static async Task<IResult> SetCredentialsAsync(
        string name,
        CredentialsRequest req,
        IScraperConfigRepository repo,
        CancellationToken ct)
    {
        var config = await repo.GetByNameAsync(name, ct);
        if (config is null) return Results.NotFound();

        config.CredentialEmail = req.Email;
        config.CredentialPassword = req.Password;
        var updated = await repo.UpsertAsync(config, ct);
        return Results.Ok(ToDto(updated));
    }

    private static ScraperConfigDto ToDto(DataSourceConfig c) => new(
        c.Name, c.Type, c.Description, c.BaseUrl, c.Enabled,
        c.MaxRequestsPerMinute, c.MinDelayMs, c.MaxDelayMs,
        c.RetryMaxAttempts, c.RetryInitialDelayMs, c.RetryBackoffMultiplier,
        !string.IsNullOrEmpty(c.CredentialEmail));
}

public record ScraperConfigDto(
    string Name,
    string Type,
    string Description,
    string BaseUrl,
    bool Enabled,
    int MaxRequestsPerMinute,
    int MinDelayMs,
    int MaxDelayMs,
    int RetryMaxAttempts,
    int RetryInitialDelayMs,
    double RetryBackoffMultiplier,
    bool HasCredentials);

public record ScraperConfigPatchRequest(
    bool? Enabled,
    string? Description,
    string? BaseUrl,
    int? MaxRequestsPerMinute,
    int? MinDelayMs,
    int? MaxDelayMs,
    int? RetryMaxAttempts);

public record CredentialsRequest(string Email, string Password);
