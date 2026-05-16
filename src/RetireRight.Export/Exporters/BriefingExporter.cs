using System.Text;
using RetireRight.Core.Interfaces;

namespace RetireRight.Export.Exporters;

public class BriefingExporter : IExporter
{
    private readonly int _maxLines;

    public BriefingExporter(int maxLines = 100) => _maxLines = maxLines;

    public string Format => "md";

    public async Task ExportAsync(ExportData data, string outputPath, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        var sb = new StringBuilder();
        var m = data.Market;

        sb.AppendLine($"# RetireRight Daily Briefing — {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();

        // Market overview
        sb.AppendLine("## Market");
        if (m.Kse100Level.HasValue)
            sb.AppendLine($"- **KSE-100:** {m.Kse100Level:N0} ({m.Kse100ChangePct:+0.00;-0.00}%)");
        if (m.PkrUsd.HasValue)
            sb.AppendLine($"- **PKR/USD:** {m.PkrUsd:N2}");
        if (m.BrentCrude.HasValue)
            sb.AppendLine($"- **Brent Crude:** ${m.BrentCrude:N2}");
        if (m.SbpRate.HasValue)
            sb.AppendLine($"- **SBP Rate:** {m.SbpRate:N2}%");
        if (m.Cpi.HasValue)
            sb.AppendLine($"- **CPI:** {m.Cpi:N2}%");
        sb.AppendLine();

        // Watchlist prices
        sb.AppendLine("## Watchlist");
        sb.AppendLine("| Symbol | Price | Chg% | Volume | Score | Signal |");
        sb.AppendLine("|--------|------:|-----:|-------:|------:|--------|");
        foreach (var s in data.Stocks.OrderByDescending(s => s.CompositeScore ?? 0))
        {
            sb.AppendLine($"| {s.Symbol} | {s.Price:N2} | {s.ChangePct:+0.00;-0.00} | {FormatVolume(s.Volume)} | {s.CompositeScore:N1} | {s.Signal ?? "-"} |");
        }
        sb.AppendLine();

        // Top Opportunities
        var opportunities = data.Stocks
            .Where(s => s.OpportunityScore >= 50)
            .OrderByDescending(s => s.OpportunityScore)
            .Take(10)
            .ToList();
        if (opportunities.Count > 0)
        {
            sb.AppendLine("## Buy Opportunities");
            sb.AppendLine("| Symbol | Price | Opp Score | Composite | Yield | Flags |");
            sb.AppendLine("|--------|------:|----------:|----------:|------:|-------|");
            foreach (var s in opportunities)
            {
                var yield = s.Metrics.GetValueOrDefault("c9_dividend_yield");
                var flags = string.Join(", ", s.OpportunityFlags.Take(3));
                sb.AppendLine($"| {s.Symbol} | {s.Price:N2} | {s.OpportunityScore:N0} | {s.CompositeScore:N0} | {yield:N1}% | {flags} |");
            }
            sb.AppendLine();
        }

        // Upcoming dividends
        if (data.Dividends.Count > 0)
        {
            sb.AppendLine("## Upcoming Dividends");
            sb.AppendLine("| Symbol | Ex-Date | DPS | Last Buy | Days |");
            sb.AppendLine("|--------|---------|----:|----------|-----:|");
            foreach (var d in data.Dividends)
            {
                sb.AppendLine($"| {d.Symbol} | {d.ExDate} | Rs. {d.Dps:N2} | {d.LastBuyDate} | {d.DaysUntilEx} |");
            }
            sb.AppendLine();
        }

        // Alerts
        if (data.Alerts.Count > 0)
        {
            sb.AppendLine("## Alerts");
            foreach (var a in data.Alerts)
            {
                var icon = a.Severity switch
                {
                    "critical" => "!!",
                    "warning" => "!",
                    "actionable" => "+",
                    "info" => "i",
                    _ => "-"
                };
                sb.AppendLine($"- [{icon}] {a.Message}");
            }
            sb.AppendLine();
        }

        // Data freshness
        sb.AppendLine("## Data Freshness");
        var f = data.Freshness;
        if (f.Prices.HasValue) sb.AppendLine($"- Prices: {f.Prices:yyyy-MM-dd HH:mm}");
        if (f.Dividends.HasValue) sb.AppendLine($"- Dividends: {f.Dividends:yyyy-MM-dd HH:mm}");
        if (f.Macro.HasValue) sb.AppendLine($"- Macro: {f.Macro:yyyy-MM-dd HH:mm}");
        foreach (var w in f.StaleWarnings)
            sb.AppendLine($"- STALE: {w}");

        // Truncate to max lines
        var lines = sb.ToString().Split('\n');
        var output = lines.Length > _maxLines
            ? string.Join('\n', lines.Take(_maxLines)) + "\n... (truncated)\n"
            : sb.ToString();

        await File.WriteAllTextAsync(outputPath, output, ct);
    }

    private static string FormatVolume(decimal? vol) => vol switch
    {
        null => "-",
        >= 1_000_000 => $"{vol / 1_000_000:N1}M",
        >= 1_000 => $"{vol / 1_000:N0}K",
        _ => $"{vol:N0}"
    };
}
