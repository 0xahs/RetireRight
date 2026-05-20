using Microsoft.EntityFrameworkCore;
using RetireRight.Api.Mapping;
using RetireRight.Core.DTOs;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Api.Endpoints;

public static class SectorsEndpoints
{
    public static void MapSectorsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sectors", GetSectorsAsync)
            .WithTags("Sectors")
            .WithName("GetSectors")
            .WithSummary("Get per-sector aggregates with top picks");
    }

    private static async Task<IResult> GetSectorsAsync(
        RetireRightDbContext db,
        CancellationToken ct)
    {
        var stocks = await db.Stocks
            .Where(s => s.IsActive)
            .AsNoTracking()
            .ToListAsync(ct);

        if (stocks.Count == 0) return Results.Ok(Array.Empty<SectorSummaryDto>());

        var stockIds = stocks.Select(s => s.Id).ToHashSet();

        // Latest computed metrics per stock — load last 60 days then deduplicate
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-60));
        var allComputed = await db.ComputedMetrics
            .Where(c => stockIds.Contains(c.StockId) && c.ComputedDate >= cutoff)
            .OrderByDescending(c => c.ComputedDate)
            .AsNoTracking()
            .ToListAsync(ct);

        var latestComputed = allComputed
            .GroupBy(c => c.StockId)
            .ToDictionary(g => g.Key, g => g.First());

        // Latest price per stock — last 7 days
        var priceCutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        var allPrices = await db.DailyPrices
            .Where(p => stockIds.Contains(p.StockId) && p.Date >= priceCutoff)
            .OrderByDescending(p => p.Date)
            .AsNoTracking()
            .ToListAsync(ct);

        var latestPrices = allPrices
            .GroupBy(p => p.StockId)
            .ToDictionary(g => g.Key, g => g.First());

        // Build per-stock summaries
        var stockData = stocks.Select(stock =>
        {
            var computed = latestComputed.GetValueOrDefault(stock.Id);
            var price    = latestPrices.GetValueOrDefault(stock.Id);
            var m        = computed?.Metrics ?? new Dictionary<string, decimal?>();
            var grammar  = SignalGrammar.Derive(m, computed, price?.UpdatedAt, stock.Sector);

            return new
            {
                stock.Id,
                stock.Symbol,
                stock.Name,
                stock.Sector,
                Price           = price?.Close,
                CompositeScore  = computed?.CompositeScore,
                DividendYield   = m.GetValueOrDefault("c9_dividend_yield"),
                PeRatio         = m.GetValueOrDefault("c1_pe_ratio"),
                OpportunityScore= m.GetValueOrDefault("o6_opportunity_score"),
                grammar.Verdict,
                grammar.ConvictionLevel,
                grammar.SafetyTier,
            };
        }).ToList();

        // Group by sector — drop sectors with no computed coverage
        var sectors = stockData
            .GroupBy(s => s.Sector)
            .Where(g => g.Any(s => s.CompositeScore.HasValue))
            .Select(g =>
            {
                var items = g.ToList();
                var withScore = items.Where(s => s.CompositeScore.HasValue).ToList();

                decimal? avgComposite = withScore.Count > 0
                    ? Math.Round(withScore.Average(s => s.CompositeScore!.Value), 1)
                    : null;

                var withYield = items.Where(s => s.DividendYield.HasValue && s.DividendYield > 0).ToList();
                decimal? avgYield = withYield.Count > 0
                    ? Math.Round(withYield.Average(s => s.DividendYield!.Value), 2)
                    : null;

                var withPe = items.Where(s => s.PeRatio.HasValue && s.PeRatio > 0 && s.PeRatio < 200).ToList();
                decimal? avgPe = withPe.Count > 0
                    ? Math.Round(withPe.Average(s => s.PeRatio!.Value), 1)
                    : null;

                var withOpp = items.Where(s => s.OpportunityScore.HasValue).ToList();
                decimal? avgOpp = withOpp.Count > 0
                    ? Math.Round(withOpp.Average(s => s.OpportunityScore!.Value), 1)
                    : null;

                var topPicks = items
                    .Where(s => s.CompositeScore.HasValue)
                    .OrderByDescending(s => s.CompositeScore)
                    .Take(3)
                    .Select(s => new SectorTopPickDto(
                        s.Symbol, s.Name, s.Price, s.CompositeScore, s.DividendYield,
                        s.Verdict, s.ConvictionLevel, s.SafetyTier))
                    .ToArray();

                return new SectorSummaryDto(
                    g.Key,
                    items.Count,
                    avgComposite,
                    avgYield,
                    avgPe,
                    avgOpp,
                    topPicks);
            })
            .OrderByDescending(s => s.AvgCompositeScore)
            .ToArray();

        return Results.Ok(sectors);
    }
}
