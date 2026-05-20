using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetireRight.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameScrapeRunSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Align historical scrape_runs.source with the renamed data_source_configs.name
            // so the UI / status lookups don't see two flavors of the same source.
            migrationBuilder.Sql(@"
                UPDATE scrape_runs SET source = 'Profile'   WHERE source = 'Sarmaaya';
                UPDATE scrape_runs SET source = 'Cash Flow' WHERE source = 'StockAnalysis';
                UPDATE scrape_runs SET source = 'Dividends' WHERE source = 'PsxTerminal';
                UPDATE scrape_runs SET source = 'Macro'     WHERE source = 'TradingEconomics';
                UPDATE scrape_runs SET source = 'Listings'  WHERE source = 'PsxUniverse';
            ");

            // Also bump the description on StockAnalysis & Listings to acknowledge the shared origin.
            migrationBuilder.Sql(@"
                UPDATE data_source_configs SET description = 'Cash flow statements (stockanalysis.com)'
                WHERE type = 'StockAnalysis';

                UPDATE data_source_configs SET description = 'PSX universe sync (stockanalysis.com)'
                WHERE type = 'PsxUniverse';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE scrape_runs SET source = 'Sarmaaya'         WHERE source = 'Profile';
                UPDATE scrape_runs SET source = 'StockAnalysis'    WHERE source = 'Cash Flow';
                UPDATE scrape_runs SET source = 'PsxTerminal'      WHERE source = 'Dividends';
                UPDATE scrape_runs SET source = 'TradingEconomics' WHERE source = 'Macro';
                UPDATE scrape_runs SET source = 'PsxUniverse'      WHERE source = 'Listings';
            ");

            migrationBuilder.Sql(@"
                UPDATE data_source_configs SET description = 'Operating & free cash flow statements'
                WHERE type = 'StockAnalysis';

                UPDATE data_source_configs SET description = 'Full PSX stock universe sync'
                WHERE type = 'PsxUniverse';
            ");
        }
    }
}
