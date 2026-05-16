using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using RetireRight.Core.Models;

#nullable disable

namespace RetireRight.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFundamentalsOwnershipScrapeRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "isin",
                table: "stocks",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "scrape_run_id",
                table: "stocks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "portfolio_transactions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "portfolio_transactions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "portfolio_holdings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "portfolio_holdings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "scrape_run_id",
                table: "macro_indicators",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "macro_indicators",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "scrape_run_id",
                table: "index_levels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "index_levels",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "percentage",
                table: "dividends",
                type: "numeric(8,4)",
                precision: 8,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "scrape_run_id",
                table: "dividends",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "dividends",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "year",
                table: "dividends",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "scrape_run_id",
                table: "daily_prices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "daily_prices",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "scrape_run_id",
                table: "computed_metrics",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "computed_metrics",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "scrape_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    mode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    symbol_count = table.Column<int>(type: "integer", nullable: false),
                    records_scraped = table.Column<int>(type: "integer", nullable: false),
                    records_failed = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrape_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "insider_transactions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stock_id = table.Column<int>(type: "integer", nullable: false),
                    person_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    position = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity = table.Column<long>(type: "bigint", nullable: true),
                    rate = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scrape_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_insider_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_insider_transactions_scrape_runs_scrape_run_id",
                        column: x => x.scrape_run_id,
                        principalTable: "scrape_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_insider_transactions_stocks_stock_id",
                        column: x => x.stock_id,
                        principalTable: "stocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_fundamentals",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stock_id = table.Column<int>(type: "integer", nullable: false),
                    scraped_date = table.Column<DateOnly>(type: "date", nullable: false),
                    periodicity = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    data = table.Column<Dictionary<string, decimal?>>(type: "jsonb", nullable: false),
                    scrape_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_fundamentals", x => x.id);
                    table.ForeignKey(
                        name: "FK_stock_fundamentals_scrape_runs_scrape_run_id",
                        column: x => x.scrape_run_id,
                        principalTable: "scrape_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stock_fundamentals_stocks_stock_id",
                        column: x => x.stock_id,
                        principalTable: "stocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_ownerships",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stock_id = table.Column<int>(type: "integer", nullable: false),
                    as_of_date = table.Column<DateOnly>(type: "date", nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    holders = table.Column<List<OwnershipEntry>>(type: "jsonb", nullable: false),
                    scrape_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_ownerships", x => x.id);
                    table.ForeignKey(
                        name: "FK_stock_ownerships_scrape_runs_scrape_run_id",
                        column: x => x.scrape_run_id,
                        principalTable: "scrape_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stock_ownerships_stocks_stock_id",
                        column: x => x.stock_id,
                        principalTable: "stocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stocks_scrape_run_id",
                table: "stocks",
                column: "scrape_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_macro_indicators_scrape_run_id",
                table: "macro_indicators",
                column: "scrape_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_index_levels_scrape_run_id",
                table: "index_levels",
                column: "scrape_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_dividends_scrape_run_id",
                table: "dividends",
                column: "scrape_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_daily_prices_scrape_run_id",
                table: "daily_prices",
                column: "scrape_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_computed_metrics_scrape_run_id",
                table: "computed_metrics",
                column: "scrape_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_insider_transactions_scrape_run_id",
                table: "insider_transactions",
                column: "scrape_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_insider_transactions_stock_id_person_name_transaction_date_~",
                table: "insider_transactions",
                columns: new[] { "stock_id", "person_name", "transaction_date", "action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_fundamentals_scrape_run_id",
                table: "stock_fundamentals",
                column: "scrape_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_fundamentals_stock_id_scraped_date_periodicity",
                table: "stock_fundamentals",
                columns: new[] { "stock_id", "scraped_date", "periodicity" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_ownerships_scrape_run_id",
                table: "stock_ownerships",
                column: "scrape_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_ownerships_stock_id_as_of_date",
                table: "stock_ownerships",
                columns: new[] { "stock_id", "as_of_date" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_computed_metrics_scrape_runs_scrape_run_id",
                table: "computed_metrics",
                column: "scrape_run_id",
                principalTable: "scrape_runs",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_daily_prices_scrape_runs_scrape_run_id",
                table: "daily_prices",
                column: "scrape_run_id",
                principalTable: "scrape_runs",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_dividends_scrape_runs_scrape_run_id",
                table: "dividends",
                column: "scrape_run_id",
                principalTable: "scrape_runs",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_index_levels_scrape_runs_scrape_run_id",
                table: "index_levels",
                column: "scrape_run_id",
                principalTable: "scrape_runs",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_macro_indicators_scrape_runs_scrape_run_id",
                table: "macro_indicators",
                column: "scrape_run_id",
                principalTable: "scrape_runs",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_stocks_scrape_runs_scrape_run_id",
                table: "stocks",
                column: "scrape_run_id",
                principalTable: "scrape_runs",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_computed_metrics_scrape_runs_scrape_run_id",
                table: "computed_metrics");

            migrationBuilder.DropForeignKey(
                name: "FK_daily_prices_scrape_runs_scrape_run_id",
                table: "daily_prices");

            migrationBuilder.DropForeignKey(
                name: "FK_dividends_scrape_runs_scrape_run_id",
                table: "dividends");

            migrationBuilder.DropForeignKey(
                name: "FK_index_levels_scrape_runs_scrape_run_id",
                table: "index_levels");

            migrationBuilder.DropForeignKey(
                name: "FK_macro_indicators_scrape_runs_scrape_run_id",
                table: "macro_indicators");

            migrationBuilder.DropForeignKey(
                name: "FK_stocks_scrape_runs_scrape_run_id",
                table: "stocks");

            migrationBuilder.DropTable(
                name: "insider_transactions");

            migrationBuilder.DropTable(
                name: "stock_fundamentals");

            migrationBuilder.DropTable(
                name: "stock_ownerships");

            migrationBuilder.DropTable(
                name: "scrape_runs");

            migrationBuilder.DropIndex(
                name: "IX_stocks_scrape_run_id",
                table: "stocks");

            migrationBuilder.DropIndex(
                name: "IX_macro_indicators_scrape_run_id",
                table: "macro_indicators");

            migrationBuilder.DropIndex(
                name: "IX_index_levels_scrape_run_id",
                table: "index_levels");

            migrationBuilder.DropIndex(
                name: "IX_dividends_scrape_run_id",
                table: "dividends");

            migrationBuilder.DropIndex(
                name: "IX_daily_prices_scrape_run_id",
                table: "daily_prices");

            migrationBuilder.DropIndex(
                name: "IX_computed_metrics_scrape_run_id",
                table: "computed_metrics");

            migrationBuilder.DropColumn(
                name: "isin",
                table: "stocks");

            migrationBuilder.DropColumn(
                name: "scrape_run_id",
                table: "stocks");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "portfolio_transactions");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "portfolio_transactions");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "portfolio_holdings");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "portfolio_holdings");

            migrationBuilder.DropColumn(
                name: "scrape_run_id",
                table: "macro_indicators");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "macro_indicators");

            migrationBuilder.DropColumn(
                name: "scrape_run_id",
                table: "index_levels");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "index_levels");

            migrationBuilder.DropColumn(
                name: "percentage",
                table: "dividends");

            migrationBuilder.DropColumn(
                name: "scrape_run_id",
                table: "dividends");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "dividends");

            migrationBuilder.DropColumn(
                name: "year",
                table: "dividends");

            migrationBuilder.DropColumn(
                name: "scrape_run_id",
                table: "daily_prices");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "daily_prices");

            migrationBuilder.DropColumn(
                name: "scrape_run_id",
                table: "computed_metrics");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "computed_metrics");
        }
    }
}
