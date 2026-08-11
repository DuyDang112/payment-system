using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankAdapter.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "bank_adapter");

            migrationBuilder.CreateTable(
                name: "provider_request_logs",
                schema: "bank_adapter",
                columns: table => new
                {
                    log_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payment_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    provider_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    operation = table.Column<int>(type: "integer", nullable: false),
                    request_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    response_received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration = table.Column<int>(type: "integer", nullable: true),
                    request_payload = table.Column<string>(type: "text", nullable: false),
                    response_payload = table.Column<string>(type: "text", nullable: false),
                    http_status_code = table.Column<int>(type: "integer", nullable: true),
                    result = table.Column<int>(type: "integer", nullable: false),
                    error_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_request_logs", x => x.log_id);
                });

            migrationBuilder.CreateTable(
                name: "providers",
                schema: "bank_adapter",
                columns: table => new
                {
                    integration_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    provider_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    provider_type = table.Column<int>(type: "integer", nullable: false),
                    api_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    endpoint = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    auth_config = table.Column<string>(type: "text", nullable: false),
                    rate_limits = table.Column<string>(type: "text", nullable: false),
                    retry_config = table.Column<string>(type: "text", nullable: false),
                    timeouts = table.Column<string>(type: "text", nullable: false),
                    supported_operations = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_providers", x => x.integration_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_provider_request_logs_operation",
                schema: "bank_adapter",
                table: "provider_request_logs",
                column: "operation");

            migrationBuilder.CreateIndex(
                name: "IX_provider_request_logs_payment_id",
                schema: "bank_adapter",
                table: "provider_request_logs",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_request_logs_provider_id",
                schema: "bank_adapter",
                table: "provider_request_logs",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_request_logs_request_sent_at",
                schema: "bank_adapter",
                table: "provider_request_logs",
                column: "request_sent_at");

            migrationBuilder.CreateIndex(
                name: "IX_provider_request_logs_result",
                schema: "bank_adapter",
                table: "provider_request_logs",
                column: "result");

            migrationBuilder.CreateIndex(
                name: "IX_providers_provider_id",
                schema: "bank_adapter",
                table: "providers",
                column: "provider_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_request_logs",
                schema: "bank_adapter");

            migrationBuilder.DropTable(
                name: "providers",
                schema: "bank_adapter");
        }
    }
}
