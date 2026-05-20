namespace RetireRight.Core.Attributes;

/// <summary>
/// Declares a scraper's type identifier and whether it operates per stock symbol.
/// The identifier is what gets stored in <c>data_source_configs.type</c> and is the
/// only place this string is hardcoded — everything else flows from DB or this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DataSourceAttribute : Attribute
{
    public string Identifier { get; }

    /// <summary>
    /// True for scrapers that fetch data per stock symbol (Sarmaaya, StockAnalysis, PsxTerminal).
    /// False for scrapers that operate on the whole universe in one go (PsxUniverse, Macro).
    /// </summary>
    public bool PerSymbol { get; init; } = true;

    public DataSourceAttribute(string identifier) => Identifier = identifier;
}
