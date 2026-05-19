using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetireRight.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeduplicateDividendsByExDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove all non-PsxTerminal dividend rows (Sarmaaya:API, etc.)
            migrationBuilder.Sql("DELETE FROM dividends WHERE source <> 'PsxTerminal';");

            // Remove remaining duplicates per (stock_id, ex_date), keeping the newest row
            migrationBuilder.Sql(@"
                DELETE FROM dividends
                WHERE id IN (
                    SELECT id FROM (
                        SELECT id,
                               ROW_NUMBER() OVER (PARTITION BY stock_id, ex_date ORDER BY created_at DESC, id DESC) AS rn
                        FROM dividends
                    ) t
                    WHERE rn > 1
                );");

            migrationBuilder.DropIndex(
                name: "IX_dividends_stock_id_ex_date_dividend_type",
                table: "dividends");

            migrationBuilder.CreateIndex(
                name: "IX_dividends_stock_id_ex_date",
                table: "dividends",
                columns: new[] { "stock_id", "ex_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_dividends_stock_id_ex_date",
                table: "dividends");

            migrationBuilder.CreateIndex(
                name: "IX_dividends_stock_id_ex_date_dividend_type",
                table: "dividends",
                columns: new[] { "stock_id", "ex_date", "dividend_type" },
                unique: true);
        }
    }
}
