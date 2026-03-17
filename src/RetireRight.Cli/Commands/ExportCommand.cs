using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RetireRight.Core.Configuration;
using RetireRight.Core.Interfaces;
using RetireRight.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace RetireRight.Cli.Commands;

public static class ExportCommand
{
    public static Command Create(IServiceProvider services)
    {
        var formatOption = new Option<string>("--format") { Description = "Export format: json, md, or all" };
        formatOption.DefaultValueFactory = _ => "all";
        var outputOption = new Option<string?>("--output") { Description = "Custom output directory" };

        var command = new Command("export") { Description = "Export pre-processed data for Claude consumption" };
        command.Options.Add(formatOption);
        command.Options.Add(outputOption);

        command.SetAction(async (parseResult, ct) =>
        {
            var format = parseResult.GetValue(formatOption)!;
            var output = parseResult.GetValue(outputOption);

            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RetireRightDbContext>();
            var exporters = scope.ServiceProvider.GetServices<IExporter>().ToList();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<RetireRightOptions>>().Value;
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var outputDir = output ?? options.Export.OutputPath;
            Directory.CreateDirectory(outputDir);

            var data = await BuildExportDataAsync(db, ct);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmm");

            foreach (var exporter in exporters)
            {
                if (format != "all" && exporter.Format != format)
                    continue;

                var filePath = Path.Combine(outputDir, $"retireright_{timestamp}.{exporter.Format}");

                try
                {
                    await exporter.ExportAsync(data, filePath, ct);
                    logger.LogInformation("Exported {Format} to {Path}", exporter.Format, filePath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to export {Format}", exporter.Format);
                }
            }

            // Write a "latest" copy for easy Claude access
            foreach (var exporter in exporters)
            {
                if (format != "all" && exporter.Format != format)
                    continue;

                var latestPath = Path.Combine(outputDir, $"latest.{exporter.Format}");
                var sourcePath = Path.Combine(outputDir, $"retireright_{timestamp}.{exporter.Format}");

                if (File.Exists(sourcePath))
                    File.Copy(sourcePath, latestPath, overwrite: true);
            }
        });

        return command;
    }

    private static async Task<ExportData> BuildExportDataAsync(RetireRightDbContext db, CancellationToken ct)
    {
        var stocks = await db.Stocks.Where(s => s.IsWatchlist && s.IsActive).ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.Now);

        var stockSnapshots = new List<StockSnapshot>();
        DateTime? latestPriceTime = null;

        foreach (var stock in stocks)
        {
            var price = await db.DailyPrices
                .Where(p => p.StockId == stock.Id)
                .OrderByDescending(p => p.Date)
                .FirstOrDefaultAsync(ct);

            var metrics = await db.ComputedMetrics
                .Where(c => c.StockId == stock.Id)
                .OrderByDescending(c => c.ComputedDate)
                .FirstOrDefaultAsync(ct);

            if (price is not null && (latestPriceTime is null || price.CreatedAt > latestPriceTime))
                latestPriceTime = price.CreatedAt;

            stockSnapshots.Add(new StockSnapshot
            {
                Symbol = stock.Symbol,
                Name = stock.Name,
                Sector = stock.Sector.ToString(),
                Price = price?.Close,
                ChangePct = price?.ChangePct,
                Volume = price?.Volume,
                Metrics = metrics?.Metrics ?? new Dictionary<string, decimal?>(),
                CompositeScore = metrics?.CompositeScore,
                Signal = metrics?.Signal.ToString()
            });
        }

        // Upcoming dividends
        var upcomingDivs = await db.Dividends
            .Include(d => d.Stock)
            .Where(d => d.ExDate >= today && d.ExDate <= today.AddDays(60))
            .OrderBy(d => d.ExDate)
            .ToListAsync(ct);

        var dividendSnapshots = upcomingDivs.Select(d => new DividendSnapshot
        {
            Symbol = d.Stock!.Symbol,
            ExDate = d.ExDate,
            Dps = d.Dps,
            PaymentDate = d.PaymentDate,
            LastBuyDate = d.ExDate.AddDays(-1),
            DaysUntilEx = d.ExDate.DayNumber - today.DayNumber
        }).ToList();

        // Market data
        var kse100 = await db.IndexLevels
            .Where(i => i.IndexName == "KSE100")
            .OrderByDescending(i => i.Date)
            .FirstOrDefaultAsync(ct);

        var pkrUsd = await db.MacroIndicators
            .Where(m => m.IndicatorName == "PKR_USD")
            .OrderByDescending(m => m.Date)
            .FirstOrDefaultAsync(ct);

        var brent = await db.MacroIndicators
            .Where(m => m.IndicatorName == "BRENT_CRUDE")
            .OrderByDescending(m => m.Date)
            .FirstOrDefaultAsync(ct);

        var sbpRate = await db.MacroIndicators
            .Where(m => m.IndicatorName == "SBP_RATE")
            .OrderByDescending(m => m.Date)
            .FirstOrDefaultAsync(ct);

        // Fundamentals freshness
        var latestFundamental = await db.StockFundamentals
            .OrderByDescending(f => f.UpdatedAt)
            .FirstOrDefaultAsync(ct);

        var staleWarnings = new List<string>();
        if (latestPriceTime.HasValue && (DateTime.UtcNow - latestPriceTime.Value).TotalHours > 24)
            staleWarnings.Add("Price data is more than 24 hours old");
        if (kse100 is not null && (today.DayNumber - kse100.Date.DayNumber) > 2)
            staleWarnings.Add("KSE-100 index data is stale");
        if (latestFundamental is not null && (DateTime.UtcNow - latestFundamental.UpdatedAt).TotalDays > 30)
            staleWarnings.Add("Fundamentals data is more than 30 days old");

        // Generate alerts
        var alerts = GenerateAlerts(stockSnapshots, dividendSnapshots);

        return new ExportData
        {
            Market = new MarketSnapshot
            {
                Kse100Level = kse100?.Close,
                Kse100ChangePct = kse100?.ChangePct,
                PkrUsd = pkrUsd?.Value,
                BrentCrude = brent?.Value,
                SbpRate = sbpRate?.Value,
                AsOf = DateTime.UtcNow
            },
            Stocks = stockSnapshots,
            Dividends = dividendSnapshots,
            Alerts = alerts,
            Freshness = new DataFreshness
            {
                Prices = latestPriceTime,
                Fundamentals = latestFundamental?.UpdatedAt,
                Macro = sbpRate?.CreatedAt,
                Dividends = upcomingDivs.Count > 0
                    ? upcomingDivs.Max(d => d.CreatedAt)
                    : null,
                StaleWarnings = staleWarnings
            }
        };
    }

    private static List<AlertSnapshot> GenerateAlerts(
        List<StockSnapshot> stocks,
        List<DividendSnapshot> dividends)
    {
        var alerts = new List<AlertSnapshot>();

        foreach (var stock in stocks)
        {
            var yield = stock.Metrics.GetValueOrDefault("c9_dividend_yield");
            var payout = stock.Metrics.GetValueOrDefault("a1_earnings_payout_ratio");

            // Yield trap: high yield + dangerously high payout
            if (yield > 10 && payout > 90)
            {
                alerts.Add(new AlertSnapshot
                {
                    Type = "YieldTrap",
                    Symbol = stock.Symbol,
                    Message = $"{stock.Symbol}: Yield {yield:F1}% with payout ratio {payout:F0}% — potential yield trap",
                    Severity = "warning"
                });
            }
            // Unsafe payout (>100% means paying more than it earns)
            else if (payout > 100)
            {
                alerts.Add(new AlertSnapshot
                {
                    Type = "UnsafePayout",
                    Symbol = stock.Symbol,
                    Message = $"{stock.Symbol}: Payout ratio {payout:F0}% exceeds earnings — unsustainable",
                    Severity = "critical"
                });
            }
        }

        // Upcoming ex-dates within 7 days
        foreach (var div in dividends.Where(d => d.DaysUntilEx <= 7 && d.DaysUntilEx >= 0))
        {
            alerts.Add(new AlertSnapshot
            {
                Type = "UpcomingExDate",
                Symbol = div.Symbol,
                Message = $"{div.Symbol}: Ex-date in {div.DaysUntilEx} day(s) on {div.ExDate:yyyy-MM-dd} — last buy date {div.LastBuyDate:yyyy-MM-dd}, DPS Rs. {div.Dps:F2}",
                Severity = "info"
            });
        }

        return alerts;
    }
}
