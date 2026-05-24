namespace RetireRight.Scrapers.Models;

/// <summary>
/// Unified output from any data-fetching strategy (API, browser, HTML parser).
/// Every field is optional — different sources provide different subsets.
/// The scraper orchestrator merges and persists whatever is available.
/// </summary>
public record ScrapedStockData
{
    public required string Symbol { get; init; }
    public required string Source { get; init; }

    // ─── Price ────────────────────────────────────────────────
    public decimal? Close { get; init; }
    public decimal? Open { get; init; }
    public decimal? High { get; init; }
    public decimal? Low { get; init; }
    public long? Volume { get; init; }
    public decimal? ChangePct { get; init; }
    public decimal? High52 { get; init; }
    public decimal? Low52 { get; init; }
    public DateOnly? PriceDate { get; init; }

    // ─── Metadata ─────────────────────────────────────────────
    public string? Name { get; init; }
    public string? Isin { get; init; }
    public string? SectorName { get; init; }
    public string? IndustryName { get; init; }

    // ─── Price History (for backfill) ─────────────────────────
    public IReadOnlyList<ScrapedPriceEntry> PriceHistory { get; init; } = [];

    // ─── Fundamentals (metric key → value) ────────────────────
    public IReadOnlyDictionary<string, decimal?> Fundamentals { get; init; } =
        new Dictionary<string, decimal?>();

    // ─── Returns (period → %) ─────────────────────────────────
    public IReadOnlyDictionary<string, decimal?> Returns { get; init; } =
        new Dictionary<string, decimal?>();

    // ─── Ownership ────────────────────────────────────────────
    public IReadOnlyList<ScrapedOwnership> Ownership { get; init; } = [];

    // ─── Insider Transactions ─────────────────────────────────
    public IReadOnlyList<ScrapedInsiderTrade> InsiderTransactions { get; init; } = [];

    // ─── Announcements ────────────────────────────────────────
    public IReadOnlyList<ScrapedAnnouncement> Announcements { get; init; } = [];

    // ─── Raw responses for archival/reprocessing ──────────────
    public IReadOnlyList<RawResponseEntry> RawResponses { get; init; } = [];
}

public record ScrapedPriceEntry
{
    public DateOnly Date { get; init; }
    public decimal Price { get; init; }
    public long? Volume { get; init; }
    public decimal? MarketCap { get; init; }
}

public record ScrapedOwnership
{
    public string? HolderName { get; init; }
    public string? HolderType { get; init; }
    public string? InvestorType { get; init; }
    public long? Shares { get; init; }
    public decimal? WeightPct { get; init; }
}

public record ScrapedInsiderTrade
{
    public string? Name { get; init; }
    public string? Position { get; init; }
    public string? Action { get; init; }
    public long? Quantity { get; init; }
    public decimal? Rate { get; init; }
    public DateOnly? Date { get; init; }
}

public record ScrapedAnnouncement
{
    public string? Title { get; init; }
    public DateOnly? PostingDate { get; init; }
    public string? Id { get; init; }
}

public record RawResponseEntry
{
    public required string Endpoint { get; init; }
    public required string Body { get; init; }
    public string ContentType { get; init; } = "application/json";
    public int? HttpStatus { get; init; }
}

// ─── Market-level data (not per-stock) ────────────────────────

public record ScrapedMarketIndex
{
    public required string Symbol { get; init; }
    public required string Name { get; init; }
    public decimal Close { get; init; }
    public decimal Change { get; init; }
    public decimal ChangePct { get; init; }
}

public record ScrapedBulkPrice
{
    public required string Symbol { get; init; }
    public decimal Price { get; init; }
    public long? Volume { get; init; }
    public decimal? ChangePct { get; init; }
    public DateOnly? Date { get; init; }
}
