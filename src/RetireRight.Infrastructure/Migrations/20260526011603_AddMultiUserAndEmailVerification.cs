using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RetireRight.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiUserAndEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "email_verified_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_admin",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "user_id",
                table: "portfolio_transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "user_id",
                table: "portfolio_holdings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "email_verification_tokens",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_verification_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_verification_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_watchlists",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    stock_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_watchlists", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_watchlists_stocks_stock_id",
                        column: x => x.stock_id,
                        principalTable: "stocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_watchlists_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_transactions_user_id",
                table: "portfolio_transactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_holdings_user_id",
                table: "portfolio_holdings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_email_verification_tokens_token",
                table: "email_verification_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_verification_tokens_user_id",
                table: "email_verification_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_watchlists_stock_id",
                table: "user_watchlists",
                column: "stock_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_watchlists_user_id_stock_id",
                table: "user_watchlists",
                columns: new[] { "user_id", "stock_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_portfolio_holdings_users_user_id",
                table: "portfolio_holdings",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_portfolio_transactions_users_user_id",
                table: "portfolio_transactions",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_portfolio_holdings_users_user_id",
                table: "portfolio_holdings");

            migrationBuilder.DropForeignKey(
                name: "FK_portfolio_transactions_users_user_id",
                table: "portfolio_transactions");

            migrationBuilder.DropTable(
                name: "email_verification_tokens");

            migrationBuilder.DropTable(
                name: "user_watchlists");

            migrationBuilder.DropIndex(
                name: "IX_portfolio_transactions_user_id",
                table: "portfolio_transactions");

            migrationBuilder.DropIndex(
                name: "IX_portfolio_holdings_user_id",
                table: "portfolio_holdings");

            migrationBuilder.DropColumn(
                name: "email_verified_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_admin",
                table: "users");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "portfolio_transactions");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "portfolio_holdings");
        }
    }
}
