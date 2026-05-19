using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RetireRight.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDataSourceConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "data_source_configs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    base_url = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    max_requests_per_minute = table.Column<int>(type: "integer", nullable: false),
                    min_delay_ms = table.Column<int>(type: "integer", nullable: false),
                    max_delay_ms = table.Column<int>(type: "integer", nullable: false),
                    retry_max_attempts = table.Column<int>(type: "integer", nullable: false),
                    retry_initial_delay_ms = table.Column<int>(type: "integer", nullable: false),
                    retry_backoff_multiplier = table.Column<double>(type: "double precision", nullable: false),
                    credential_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    credential_password = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_source_configs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_data_source_configs_name",
                table: "data_source_configs",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "data_source_configs");
        }
    }
}
