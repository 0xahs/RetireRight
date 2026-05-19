using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Scrapers;

namespace RetireRight.Cli.Commands;

public static class RunCommand
{
    public static Command Create(IServiceProvider services)
    {
        var allOption = new Option<bool>("--all") { Description = "Run all enabled scrapers" };
        var sourceOption = new Option<string?>("--source") { Description = "Run a specific source (e.g., Sarmaaya, PsxTerminal)" };
        var symbolsOption = new Option<string[]>("--symbols")
        {
            Description = "Specific symbols to scrape",
            AllowMultipleArgumentsPerToken = true
        };
        var testOption = new Option<bool>("--test") { Description = "Mark this run as test data (can be bulk-deleted later)" };

        var command = new Command("run") { Description = "Run scrapers to fetch latest PSX data" };
        command.Options.Add(allOption);
        command.Options.Add(sourceOption);
        command.Options.Add(symbolsOption);
        command.Options.Add(testOption);

        command.SetAction(async (parseResult, ct) =>
        {
            var all = parseResult.GetValue(allOption);
            var source = parseResult.GetValue(sourceOption);
            var symbols = parseResult.GetValue(symbolsOption);
            var isTest = parseResult.GetValue(testOption);

            await using var scope = services.CreateAsyncScope();
            var factory = scope.ServiceProvider.GetRequiredService<ScraperFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var scrapeRunRepo = scope.ServiceProvider.GetRequiredService<IScrapeRunRepository>();

            var scrapers = await factory.CreateEnabledAsync(all ? null : source, ct);

            if (!all && source is null)
            {
                logger.LogError("Specify --all to run all scrapers or --source <name> for a specific one");
                return;
            }

            logger.LogInformation("Running {Count} scraper(s) in {Mode} mode...",
                scrapers.Count, isTest ? "TEST" : "LIVE");

            foreach (var scraper in scrapers)
            {
                var run = await scrapeRunRepo.CreateAsync(new ScrapeRun
                {
                    Source = scraper.Source.ToString(),
                    Mode = isTest ? "test" : "live",
                    SymbolCount = symbols?.Length ?? 0
                }, ct);

                var request = new ScrapeRequest
                {
                    Symbols = symbols ?? [],
                    ScrapeRunId = run.Id
                };

                try
                {
                    var result = await scraper.ScrapeAsync(request, ct);

                    run.CompletedAt = DateTime.UtcNow;
                    run.RecordsScraped = result.RecordsScraped;
                    run.RecordsFailed = result.RecordsFailed;
                    run.Status = result.Success ? "completed" : "failed";
                    await scrapeRunRepo.UpdateAsync(run, ct);

                    var status = result.Success ? "OK" : "FAILED";
                    logger.LogInformation("[{Status}] {Source}: {Scraped} scraped, {Failed} failed in {Duration}ms",
                        status, result.Source, result.RecordsScraped, result.RecordsFailed,
                        result.Duration.TotalMilliseconds);

                    foreach (var error in result.Errors)
                        logger.LogWarning("  Error: {Error}", error);
                }
                catch (Exception ex)
                {
                    run.CompletedAt = DateTime.UtcNow;
                    run.Status = "failed";
                    await scrapeRunRepo.UpdateAsync(run, ct);
                    logger.LogError(ex, "Scraper {Source} failed", scraper.Source);
                }
                finally
                {
                    await scraper.DisposeAsync();
                }
            }
        });

        return command;
    }
}
