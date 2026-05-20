using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetireRight.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameDataSourceConfigsToFunctional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE data_source_configs SET name = 'Profile',
                    description = 'Prices, fundamentals, ownership, insiders'
                WHERE type = 'Sarmaaya';

                UPDATE data_source_configs SET name = 'Cash Flow',
                    description = 'Operating & free cash flow statements'
                WHERE type = 'StockAnalysis';

                UPDATE data_source_configs SET name = 'Dividends',
                    description = 'Historical dividend payments (6+ years)'
                WHERE type = 'PsxTerminal';

                UPDATE data_source_configs SET name = 'Macro',
                    description = 'PKR/USD, Brent crude, SBP policy rate'
                WHERE type = 'TradingEconomics';

                UPDATE data_source_configs SET name = 'Listings',
                    description = 'Full PSX stock universe sync'
                WHERE type = 'PsxUniverse';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE data_source_configs SET name = 'Sarmaaya',
                    description = 'Prices, fundamentals, ownership, insiders'
                WHERE type = 'Sarmaaya';

                UPDATE data_source_configs SET name = 'StockAnalysis',
                    description = 'Cash flow metrics'
                WHERE type = 'StockAnalysis';

                UPDATE data_source_configs SET name = 'PsxTerminal',
                    description = 'Dividend history (6+ years)'
                WHERE type = 'PsxTerminal';

                UPDATE data_source_configs SET name = 'TradingEconomics',
                    description = 'PKR/USD, Brent crude, SBP rate'
                WHERE type = 'TradingEconomics';

                UPDATE data_source_configs SET name = 'PsxUniverse',
                    description = 'Full PSX stock universe from StockAnalysis listing page'
                WHERE type = 'PsxUniverse';
            ");
        }
    }
}
