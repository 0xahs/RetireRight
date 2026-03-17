namespace RetireRight.Scrapers.AntiBot;

/// <summary>
/// Randomizes HTTP request headers per-request to avoid fingerprint-based bot detection.
/// Real browsers vary these headers subtly — uniform headers are a bot signature.
/// </summary>
public static class RequestFingerprint
{
    private static readonly Random Rng = new();

    private static readonly string[][] AcceptEncodings =
    [
        ["gzip", "deflate", "br"],
        ["gzip", "deflate"],
        ["gzip", "deflate", "br", "zstd"],
        ["gzip", "br"],
    ];

    private static readonly string[] AcceptLanguages =
    [
        "en-US,en;q=0.9",
        "en-US,en;q=0.9,ur;q=0.8",
        "en-GB,en;q=0.9,en-US;q=0.8",
        "en;q=0.9,ur-PK;q=0.8",
        "en-US,en;q=0.8",
        "en-PK,en;q=0.9,en-US;q=0.8",
    ];

    private static readonly string[] SecFetchSite = ["same-site", "cross-site", "same-origin"];
    private static readonly string[] SecFetchMode = ["cors", "no-cors"];
    private static readonly string[] ConnectionValues = ["keep-alive", "Keep-Alive"];

    /// <summary>
    /// Applies randomized browser-like headers to an outgoing request.
    /// Call this on every HttpRequestMessage before sending.
    /// </summary>
    public static void Apply(HttpRequestMessage request)
    {
        // Vary Accept-Encoding combination
        request.Headers.TryAddWithoutValidation("Accept-Encoding",
            string.Join(", ", AcceptEncodings[Rng.Next(AcceptEncodings.Length)]));

        // Vary Accept-Language
        request.Headers.TryAddWithoutValidation("Accept-Language",
            AcceptLanguages[Rng.Next(AcceptLanguages.Length)]);

        // Connection header (most browsers send this)
        request.Headers.TryAddWithoutValidation("Connection",
            ConnectionValues[Rng.Next(ConnectionValues.Length)]);

        // Randomly include Sec-Fetch-* headers (Chrome/Edge send these, Firefox doesn't always)
        if (Rng.NextDouble() > 0.3)
        {
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", SecFetchSite[Rng.Next(SecFetchSite.Length)]);
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", SecFetchMode[Rng.Next(SecFetchMode.Length)]);
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "empty");
        }

        // DNT header — some browsers send it, some don't
        if (Rng.NextDouble() > 0.5)
            request.Headers.TryAddWithoutValidation("DNT", "1");

        // Cache-Control — varies with browser state
        if (Rng.NextDouble() > 0.6)
            request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache");

        // Sec-CH-UA hints (Chromium-based browsers)
        if (Rng.NextDouble() > 0.4)
        {
            request.Headers.TryAddWithoutValidation("Sec-CH-UA-Platform", "\"Windows\"");
            request.Headers.TryAddWithoutValidation("Sec-CH-UA-Mobile", "?0");
        }
    }
}
