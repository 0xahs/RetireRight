using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetireRight.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceUrlsToDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Surface the underlying source domain on every description so the user can
            // see at a glance which site each scraper pulls from.
            migrationBuilder.Sql(@"
                UPDATE data_source_configs SET description = 'Prices, fundamentals, ownership, insiders (sarmaaya.pk)'
                WHERE type = 'Sarmaaya';

                UPDATE data_source_configs SET description = 'Cash flow statements (stockanalysis.com)'
                WHERE type = 'StockAnalysis';

                UPDATE data_source_configs SET description = 'Dividend history (psxterminal.com)'
                WHERE type = 'PsxTerminal';

                UPDATE data_source_configs SET description = 'PKR/USD, Brent crude, SBP rate (tradingeconomics.com)'
                WHERE type = 'TradingEconomics';

                UPDATE data_source_configs SET description = 'PSX universe sync (stockanalysis.com)'
                WHERE type = 'PsxUniverse';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE data_source_configs SET description = 'Prices, fundamentals, ownership, insiders'
                WHERE type = 'Sarmaaya';

                UPDATE data_source_configs SET description = 'Cash flow statements (stockanalysis.com)'
                WHERE type = 'StockAnalysis';

                UPDATE data_source_configs SET description = 'Historical dividend payments (6+ years)'
                WHERE type = 'PsxTerminal';

                UPDATE data_source_configs SET description = 'PKR/USD, Brent crude, SBP policy rate'
                WHERE type = 'TradingEconomics';

                UPDATE data_source_configs SET description = 'PSX universe sync (stockanalysis.com)'
                WHERE type = 'PsxUniverse';
            ");
        }
    }
}
