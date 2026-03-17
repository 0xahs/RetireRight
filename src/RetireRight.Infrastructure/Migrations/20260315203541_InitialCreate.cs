using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RetireRight.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "index_levels",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    index_name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    open = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    high = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    low = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    close = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    volume = table.Column<long>(type: "bigint", nullable: true),
                    change_pct = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_index_levels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "macro_indicators",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    indicator_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    value = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_macro_indicators", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "raw_responses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    response_body = table.Column<string>(type: "text", nullable: false),
                    content_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    http_status = table.Column<int>(type: "integer", nullable: true),
                    scraped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_responses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stocks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sector = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    listed_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_watchlist = table.Column<bool>(type: "boolean", nullable: false),
                    circuit_limit_pct = table.Column<decimal>(type: "numeric", nullable: true),
                    lot_size = table.Column<int>(type: "integer", nullable: false),
                    trading_board = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stocks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "computed_metrics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stock_id = table.Column<int>(type: "integer", nullable: false),
                    computed_date = table.Column<DateOnly>(type: "date", nullable: false),
                    metrics = table.Column<Dictionary<string, decimal?>>(type: "jsonb", nullable: false),
                    composite_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    dividend_safety_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    dividend_growth_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    valuation_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    signal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_computed_metrics", x => x.id);
                    table.ForeignKey(
                        name: "FK_computed_metrics_stocks_stock_id",
                        column: x => x.stock_id,
                        principalTable: "stocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "daily_prices",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stock_id = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    open = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    high = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    low = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    close = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    volume = table.Column<long>(type: "bigint", nullable: true),
                    trades = table.Column<int>(type: "integer", nullable: true),
                    value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    change_pct = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_prices", x => x.id);
                    table.ForeignKey(
                        name: "FK_daily_prices_stocks_stock_id",
                        column: x => x.stock_id,
                        principalTable: "stocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dividends",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stock_id = table.Column<int>(type: "integer", nullable: false),
                    ex_date = table.Column<DateOnly>(type: "date", nullable: false),
                    record_date = table.Column<DateOnly>(type: "date", nullable: true),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: true),
                    dps = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    dividend_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dividends", x => x.id);
                    table.ForeignKey(
                        name: "FK_dividends_stocks_stock_id",
                        column: x => x.stock_id,
                        principalTable: "stocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "portfolio_holdings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stock_id = table.Column<int>(type: "integer", nullable: false),
                    shares = table.Column<int>(type: "integer", nullable: false),
                    avg_cost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    purchase_date = table.Column<DateOnly>(type: "date", nullable: true),
                    platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolio_holdings", x => x.id);
                    table.ForeignKey(
                        name: "FK_portfolio_holdings_stocks_stock_id",
                        column: x => x.stock_id,
                        principalTable: "stocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "portfolio_transactions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stock_id = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    shares = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    commission = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolio_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_portfolio_transactions_stocks_stock_id",
                        column: x => x.stock_id,
                        principalTable: "stocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_computed_metrics_stock_id_computed_date",
                table: "computed_metrics",
                columns: new[] { "stock_id", "computed_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_prices_stock_id_date",
                table: "daily_prices",
                columns: new[] { "stock_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dividends_stock_id_ex_date_dividend_type",
                table: "dividends",
                columns: new[] { "stock_id", "ex_date", "dividend_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_index_levels_index_name_date",
                table: "index_levels",
                columns: new[] { "index_name", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_macro_indicators_indicator_name_date",
                table: "macro_indicators",
                columns: new[] { "indicator_name", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_holdings_stock_id",
                table: "portfolio_holdings",
                column: "stock_id");

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_transactions_stock_id",
                table: "portfolio_transactions",
                column: "stock_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocks_sector",
                table: "stocks",
                column: "sector");

            migrationBuilder.CreateIndex(
                name: "IX_stocks_symbol",
                table: "stocks",
                column: "symbol",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "computed_metrics");

            migrationBuilder.DropTable(
                name: "daily_prices");

            migrationBuilder.DropTable(
                name: "dividends");

            migrationBuilder.DropTable(
                name: "index_levels");

            migrationBuilder.DropTable(
                name: "macro_indicators");

            migrationBuilder.DropTable(
                name: "portfolio_holdings");

            migrationBuilder.DropTable(
                name: "portfolio_transactions");

            migrationBuilder.DropTable(
                name: "raw_responses");

            migrationBuilder.DropTable(
                name: "stocks");
        }
    }
}
