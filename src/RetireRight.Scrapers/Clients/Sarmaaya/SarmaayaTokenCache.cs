using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace RetireRight.Scrapers.Clients.Sarmaaya;

public class SarmaayaTokenCache
{
    private static readonly string CachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RetireRight", "sarmaaya_token.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }

    public bool IsValid =>
        AccessToken is not null &&
        DateTimeOffset.UtcNow.ToUnixTimeSeconds() < ExpiresAt - 600;

    public static SarmaayaTokenCache? Load(ILogger? logger = null)
    {
        try
        {
            if (!File.Exists(CachePath)) return null;
            var json = File.ReadAllText(CachePath);
            return JsonSerializer.Deserialize<SarmaayaTokenCache>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to read Sarmaaya token cache — will re-login");
            return null;
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CachePath)!);
        File.WriteAllText(CachePath, JsonSerializer.Serialize(this, JsonOptions));
    }
}
