namespace RetireRight.Core.Models;

public class RawApiResponse
{
    public long Id { get; set; }
    public required string Source { get; set; }
    public required string Endpoint { get; set; }
    public string? Symbol { get; set; }
    public required string ResponseBody { get; set; }
    public string? ContentType { get; set; }
    public int? HttpStatus { get; set; }
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
}
