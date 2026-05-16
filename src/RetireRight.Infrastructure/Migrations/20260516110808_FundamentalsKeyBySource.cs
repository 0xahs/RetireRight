using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetireRight.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FundamentalsKeyBySource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_fundamentals_stock_id_scraped_date_periodicity",
                table: "stock_fundamentals");

            migrationBuilder.CreateIndex(
                name: "IX_stock_fundamentals_stock_id_periodicity_source",
                table: "stock_fundamentals",
                columns: new[] { "stock_id", "periodicity", "source" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_fundamentals_stock_id_periodicity_source",
                table: "stock_fundamentals");

            migrationBuilder.CreateIndex(
                name: "IX_stock_fundamentals_stock_id_scraped_date_periodicity",
                table: "stock_fundamentals",
                columns: new[] { "stock_id", "scraped_date", "periodicity" },
                unique: true);
        }
    }
}
