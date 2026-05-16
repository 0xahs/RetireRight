using RetireRight.Core.DTOs;
using RetireRight.MetricsEngine;
using RetireRight.Screening;

namespace RetireRight.Api.Endpoints;

public static class ScreenerEndpoints
{
    public static void MapScreenerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/screener").WithTags("Screener");

        group.MapGet("/presets", () =>
            Results.Ok(new[] { "dividend_safety", "high_yield", "growth", "value", "retirement_core" }))
            .WithName("GetScreenerPresets");

        group.MapPost("/run", RunScreenerAsync)
            .WithName("RunScreener");
    }

    private static async Task<IResult> RunScreenerAsync(
        ScreenerRequestDto request,
        IScreener screener,
        CancellationToken ct)
    {
        var criteria = new ScreenCriteria
        {
            Preset = request.Preset,
            SortBy = request.SortBy,
            Ascending = request.Ascending,
            Limit = Math.Clamp(request.Limit, 1, 200),
            WatchlistOnly = request.WatchlistOnly
        };

        if (request.Filters is { Length: > 0 })
        {
            foreach (var f in request.Filters)
                criteria.Filters[f.MetricKey] = (f.Min, f.Max);
        }

        var result = await screener.ScreenAsync(criteria, ct);

        var stocks = result.Stocks
            .Select(s => new ScreenerStockDto(
                Symbol: s.Symbol,
                Name: s.Name,
                Sector: s.Sector ?? "",
                Signal: s.Signal,
                CompositeScore: s.CompositeScore,
                OpportunityScore: s.SelectedMetrics.GetValueOrDefault("o6_opportunity_score"),
                DividendYield: s.SelectedMetrics.GetValueOrDefault("c9_dividend_yield"),
                DividendCoverage: s.SelectedMetrics.GetValueOrDefault("a4_dividend_coverage"),
                PeRatio: s.SelectedMetrics.GetValueOrDefault("c1_pe_ratio"),
                OpportunityFlags: OpportunityScorer.DecodeFlags(s.SelectedMetrics.GetValueOrDefault("o7_flags")).ToArray(),
                Rank: s.Rank))
            .ToArray();

        return Results.Ok(new ScreenerResultDto(
            stocks,
            result.TotalMatched,
            result.TotalEvaluated,
            result.AppliedPreset));
    }
}
