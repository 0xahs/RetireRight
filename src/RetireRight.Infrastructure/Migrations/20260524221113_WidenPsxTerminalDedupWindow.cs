using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetireRight.Infrastructure.Migrations
{
    /// <summary>
    /// Widens the cross-source dedup window from ±2 to ±3 days and re-applies it to
    /// existing data. Observed AABS 2026-05 had a 3-day gap (PT May 29, SA May 26)
    /// that the prior ±2 cleanup missed. Same delete rule: keep StockAnalysisDividends
    /// (sourced from S&P Global Market Intelligence), drop the matching PsxTerminal row.
    /// </summary>
    public partial class WidenPsxTerminalDedupWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM dividends pt
                USING dividends sa
                WHERE pt.source = 'PsxTerminal'
                  AND sa.source = 'StockAnalysisDividends'
                  AND sa.stock_id = pt.stock_id
                  AND ABS(sa.ex_date - pt.ex_date) <= 3;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: data cleanup is not meaningfully reversible.
        }
    }
}
