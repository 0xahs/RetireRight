using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetireRight.Infrastructure.Migrations
{
    /// <summary>
    /// One-shot cleanup for PsxTerminal dividend rows:
    ///   1. The per-stock /__data.json endpoint returns NET-of-WHT values (×0.85). They
    ///      had been stored as if gross. Gross them up to match the rest of the system's
    ///      gross-DPS convention.
    ///   2. Cross-source date drift (PT records Jun 5, StockAnalysis records Jun 6 for the
    ///      same dividend event). Delete PsxTerminal rows that have a StockAnalysisDividends
    ///      row for the same stock within ±2 days — StockAnalysisDividends (sourced from
    ///      S&P Global Market Intelligence) is the date-convention authority.
    /// </summary>
    public partial class CleanupPsxTerminalDividends : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: gross-up existing PsxTerminal rows (net → gross, /0.85).
            migrationBuilder.Sql(@"
                UPDATE dividends
                SET dps = ROUND(dps / 0.85, 4),
                    updated_at = NOW()
                WHERE source = 'PsxTerminal';
            ");

            // Step 2: delete PsxTerminal rows where a StockAnalysisDividends row exists
            // for the same stock within ±2 days (same event, different recorded ex-date).
            migrationBuilder.Sql(@"
                DELETE FROM dividends pt
                USING dividends sa
                WHERE pt.source = 'PsxTerminal'
                  AND sa.source = 'StockAnalysisDividends'
                  AND sa.stock_id = pt.stock_id
                  AND ABS(sa.ex_date - pt.ex_date) <= 2;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down is intentionally a no-op: this migration is a data cleanup, not a
            // schema change. Reverting it would mean re-introducing the wrong values
            // and duplicates, which makes no sense. The next scrape will repopulate
            // anything that was legitimately deleted from authoritative sources.
        }
    }
}
