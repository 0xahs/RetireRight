using RetireRight.Core.DTOs;
using RetireRight.Core.Interfaces;

namespace RetireRight.Api.Endpoints;

public static class DividendsEndpoints
{
    public static void MapDividendsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dividends").WithTags("Dividends");

        group.MapGet("/upcoming", GetUpcomingAsync)
            .WithName("GetUpcomingDividends");
    }

    private static async Task<IResult> GetUpcomingAsync(
        int days,
        IDividendRepository dividendRepo,
        IStockRepository stockRepo,
        CancellationToken ct)
    {
        days = Math.Clamp(days, 1, 365);
        var upcoming = await dividendRepo.GetUpcomingAsync(days, ct);
        var today = DateOnly.FromDateTime(DateTime.Now);

        var result = upcoming
            .Where(d => d.Stock is not null)
            .Select(d => new UpcomingDividendDto(
                Symbol: d.Stock!.Symbol,
                StockName: d.Stock.Name,
                Sector: d.Stock.Sector,
                ExDate: d.ExDate,
                LastBuyDate: d.ExDate.AddDays(-1),
                PaymentDate: d.PaymentDate,
                Dps: d.Dps,
                DaysUntilEx: d.ExDate.DayNumber - today.DayNumber))
            .OrderBy(d => d.DaysUntilEx)
            .ToArray();

        return Results.Ok(result);
    }
}
