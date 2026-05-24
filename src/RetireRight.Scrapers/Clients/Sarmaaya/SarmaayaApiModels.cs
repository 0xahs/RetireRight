using System.Text.Json;
using System.Text.Json.Serialization;

namespace RetireRight.Scrapers.Clients.Sarmaaya;

/// <summary>
/// Standard Sarmaaya API envelope. Every endpoint returns { success, message, response }.
/// </summary>
internal class SarmaayaEnvelope<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Response { get; set; }
}

// ─── /api/stocks/{SYMBOL} ─────────────────────────────────────

internal class SarmaayaStockResponse
{
    public string? Isin { get; set; }
    public string? Name { get; set; }
    public string? Symbol { get; set; }
    public decimal Close { get; set; }
    public decimal? Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public long Volume { get; set; }
    public decimal Change { get; set; }
    [JsonPropertyName("change_percentage")]
    public decimal ChangePercentage { get; set; }
    [JsonPropertyName("high52")]
    public decimal? High52 { get; set; }
    [JsonPropertyName("low52")]
    public decimal? Low52 { get; set; }
    public string? SectorCategory { get; set; }
    public string? SectorName { get; set; }
    public string? Date { get; set; }
    [JsonPropertyName("isshariah")]
    public bool? IsShariah { get; set; }
}

// ─── /api/stocks/about?symbol={SYMBOL} ────────────────────────

internal class SarmaayaAboutResponse
{
    public string? Isin { get; set; }
    public string? Sector { get; set; }
    public string? Industry { get; set; }
    public string? Website { get; set; }
    public string? Ceo { get; set; }
    [JsonPropertyName("company_size")]
    public int? CompanySize { get; set; }
}

// ─── /api/stocks/price-history/{SYMBOL}?days=N ────────────────

internal class SarmaayaPriceHistoryEntry
{
    public string? Symbol { get; set; }
    public decimal Price { get; set; }
    public string? Date { get; set; }
    public long? Volume { get; set; }
    public decimal? MarketCap { get; set; }
}

// ─── /api/stocks/returns/{SYMBOL} ─────────────────────────────
// Returns is a list with dynamic numeric keys ("1w", "1m", etc.)
// We deserialize as JsonElement and extract manually.

// ─── /api/stocks/fundamentals/ratios?isin={ISIN}&periodicity=ANN
// ─── /api/stocks/fundamentals/income-statement?isin={ISIN}&periodicity=ANN
// Both return Dictionary<displayName, FundamentalMetric>

internal class SarmaayaFundamentalMetric
{
    public string? Metric { get; set; }
    public string? DisplayType { get; set; }
    public List<SarmaayaFundamentalDataPoint>? Data { get; set; }
}

internal class SarmaayaFundamentalDataPoint
{
    public JsonElement Value { get; set; }
    public string? Date { get; set; }
    public int? Year { get; set; }
}

// ─── /api/stocks/ownership/equity-holders/{ISIN} ─────────────
// API returns response as a direct array of holders (not wrapped in an object).
// Field "weightClose" = ownership percentage.

internal class SarmaayaOwnershipEntry
{
    public string? HolderName { get; set; }
    public string? HolderType { get; set; }
    public string? InvestorType { get; set; }
    public long? Shares { get; set; }
    public decimal? WeightClose { get; set; }
}

// ─── /api/stocks/stock-insiders/{SYMBOL} ─────────────────────

internal class SarmaayaInsiderEntry
{
    public string? Name { get; set; }
    public string? Position { get; set; }
    public string? Action { get; set; }
    public long? Quantity { get; set; }
    public decimal? Rate { get; set; }
    public string? Date { get; set; }
}

// ─── /api/stocks/announcements/{SYMBOL}?startDate=...&endDate=...

internal class SarmaayaAnnouncementEntry
{
    public string? Title { get; set; }
    [JsonPropertyName("posting_date")]
    public string? PostingDate { get; set; }
    public string? Id { get; set; }
}

// ─── /api/stocks/fundamentals/industry/{ISIN} ────────────────
// Returns { response: { industryAverages: [ { name, average, min, max } ] } }

internal class SarmaayaIndustryResponse
{
    public List<SarmaayaIndustryAverage>? IndustryAverages { get; set; }
}

internal class SarmaayaIndustryAverage
{
    public string? Name { get; set; }
    public decimal? Average { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
}

// ─── /api/stocks/peers/{sectorId} ────────────────────────────

internal class SarmaayaPeerEntry
{
    public string? Symbol { get; set; }
    public string? Name { get; set; }
    public decimal? Close { get; set; }
    public decimal? Change { get; set; }
    [JsonPropertyName("change_percentage")]
    public decimal? ChangePercentage { get; set; }
    public long? Volume { get; set; }
}

// ─── /api/dashboard/market-view ───────────────────────────────

internal class SarmaayaMarketIndex
{
    public string? Name { get; set; }
    public string? Symbol { get; set; }
    public decimal Close { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercentage { get; set; }
}

// ─── /api/stocks/ticker?index=KSE100 ─────────────────────────

internal class SarmaayaTickerEntry
{
    public string? Symbol { get; set; }
    public decimal Price { get; set; }
    public long? Volume { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercentage { get; set; }
    public bool? IsShariah { get; set; }
    public string? Date { get; set; }
}
