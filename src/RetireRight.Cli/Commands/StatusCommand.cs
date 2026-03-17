using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using RetireRight.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace RetireRight.Cli.Commands;

public static class StatusCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("status") { Description = "Show database status and data freshness" };

        command.SetAction(async (parseResult, ct) =>
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RetireRightDbContext>();

            Console.WriteLine("=== RetireRight Status ===");
            Console.WriteLine();

            int stockCount;
            try
            {
                stockCount = await db.Stocks.CountAsync(ct);
                Console.WriteLine("[OK] Database connection");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAIL] Database connection: {ex.Message}");
                return;
            }
            var watchlistCount = await db.Stocks.CountAsync(s => s.IsWatchlist, ct);
            var priceCount = await db.DailyPrices.CountAsync(ct);
            var dividendCount = await db.Dividends.CountAsync(ct);
            var macroCount = await db.MacroIndicators.CountAsync(ct);
            var rawCount = await db.RawResponses.CountAsync(ct);

            Console.WriteLine($"Stocks:      {stockCount} total, {watchlistCount} on watchlist");
            Console.WriteLine($"Prices:      {priceCount} records");
            Console.WriteLine($"Dividends:   {dividendCount} records");
            Console.WriteLine($"Macro:       {macroCount} records");
            Console.WriteLine($"Raw HTML:    {rawCount} stored");
            Console.WriteLine();

            var latestPrice = await db.DailyPrices
                .OrderByDescending(p => p.Date)
                .Select(p => p.Date)
                .FirstOrDefaultAsync(ct);

            var latestMacro = await db.MacroIndicators
                .OrderByDescending(m => m.Date)
                .Select(m => m.Date)
                .FirstOrDefaultAsync(ct);

            Console.WriteLine("Latest Data:");
            Console.WriteLine($"  Prices:    {(latestPrice == default ? "No data" : latestPrice.ToString())}");
            Console.WriteLine($"  Macro:     {(latestMacro == default ? "No data" : latestMacro.ToString())}");
            Console.WriteLine();

            var watchlist = await db.Stocks
                .Where(s => s.IsWatchlist)
                .OrderBy(s => s.Symbol)
                .Select(s => s.Symbol)
                .ToListAsync(ct);

            if (watchlist.Count > 0)
                Console.WriteLine($"Watchlist:   {string.Join(", ", watchlist)}");
        });

        return command;
    }
}
