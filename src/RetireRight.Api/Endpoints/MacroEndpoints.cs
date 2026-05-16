using RetireRight.Core.DTOs;
using RetireRight.Core.Interfaces;

namespace RetireRight.Api.Endpoints;

public static class MacroEndpoints
{
    public static void MapMacroEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/macro").WithTags("Macro");

        group.MapGet("/", GetMacroAsync)
            .WithName("GetMacro");
    }

    private static async Task<IResult> GetMacroAsync(
        IMacroRepository macroRepo,
        IIndexLevelRepository indexRepo,
        CancellationToken ct)
    {
        var kse100 = await indexRepo.GetLatestAsync("KSE100", ct);
        var pkrUsd = await macroRepo.GetLatestAsync("PKR_USD", ct);
        var brent = await macroRepo.GetLatestAsync("BRENT_CRUDE", ct);
        var sbpRate = await macroRepo.GetLatestAsync("SBP_RATE", ct);
        var cpi = await macroRepo.GetLatestAsync("CPI", ct);

        return Results.Ok(new MacroDto(
            Kse100Level: kse100?.Close,
            Kse100ChangePct: kse100?.ChangePct,
            SbpRate: sbpRate?.Value,
            PkrUsd: pkrUsd?.Value,
            BrentCrude: brent?.Value,
            Cpi: cpi?.Value,
            AsOf: DateTime.UtcNow));
    }
}
