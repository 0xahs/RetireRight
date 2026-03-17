using System.Text.Json;
using System.Text.Json.Serialization;
using RetireRight.Core.Interfaces;

namespace RetireRight.Export.Exporters;

public class JsonExporter : IExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public string Format => "json";

    public async Task ExportAsync(ExportData data, string outputPath, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(data, JsonOptions);
        await File.WriteAllTextAsync(outputPath, json, ct);
    }
}
