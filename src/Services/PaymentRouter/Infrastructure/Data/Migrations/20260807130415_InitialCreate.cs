using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentRouter.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payment_router");

            migrationBuilder.CreateTable(
                name: "payment_providers",
                schema: "payment_router",
                columns: table => new
                {
                    ProviderId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProviderType = table.Column<string>(type: "text", nullable: false),
                    SupportedCurrencies = table.Column<string[]>(type: "text[]", nullable: false),
                    SupportedMethods = table.Column<string[]>(type: "text[]", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MinAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CostConfigJson = table.Column<string>(type: "jsonb", nullable: false),
                    MetricsJson = table.Column<string>(type: "jsonb", nullable: false),
                    HealthStatus = table.Column<string>(type: "text", nullable: false),
                    CircuitBreakerState = table.Column<string>(type: "text", nullable: false),
                    LastHealthCheck = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_providers", x => x.ProviderId);
                });

            migrationBuilder.CreateTable(
                name: "routing_decisions",
                schema: "payment_router",
                columns: table => new
                {
                    DecisionId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MerchantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SelectedProviderId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AlternativeProviderIds = table.Column<string[]>(type: "text[]", nullable: false),
                    Strategy = table.Column<string>(type: "text", nullable: false),
                    DecisionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CostEstimateJson = table.Column<string>(type: "jsonb", nullable: false),
                    DecisionMadeAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaymentMethod = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routing_decisions", x => x.DecisionId);
                });

            migrationBuilder.CreateTable(
                name: "routing_rules",
                schema: "payment_router",
                columns: table => new
                {
                    RuleId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MerchantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ConditionsJson = table.Column<string>(type: "jsonb", nullable: false),
                    PreferredProviderIds = table.Column<string[]>(type: "text[]", nullable: false),
                    Strategy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routing_rules", x => x.RuleId);
                });

            migrationBuilder.CreateIndex(
                name: "idx_payment_providers_health_status",
                schema: "payment_router",
                table: "payment_providers",
                column: "HealthStatus");

            migrationBuilder.CreateIndex(
                name: "idx_payment_providers_is_enabled",
                schema: "payment_router",
                table: "payment_providers",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "idx_payment_providers_priority",
                schema: "payment_router",
                table: "payment_providers",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "idx_routing_decisions_decision_made_at",
                schema: "payment_router",
                table: "routing_decisions",
                column: "DecisionMadeAt");

            migrationBuilder.CreateIndex(
                name: "idx_routing_decisions_merchant_id",
                schema: "payment_router",
                table: "routing_decisions",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "idx_routing_decisions_payment_id",
                schema: "payment_router",
                table: "routing_decisions",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "idx_routing_decisions_selected_provider",
                schema: "payment_router",
                table: "routing_decisions",
                column: "SelectedProviderId");

            migrationBuilder.CreateIndex(
                name: "idx_routing_rules_is_active",
                schema: "payment_router",
                table: "routing_rules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "idx_routing_rules_merchant_id",
                schema: "payment_router",
                table: "routing_rules",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "idx_routing_rules_priority",
                schema: "payment_router",
                table: "routing_rules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "uq_routing_rules_merchant_name",
                schema: "payment_router",
                table: "routing_rules",
                columns: new[] { "MerchantId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_providers",
                schema: "payment_router");

            migrationBuilder.DropTable(
                name: "routing_decisions",
                schema: "payment_router");

            migrationBuilder.DropTable(
                name: "routing_rules",
                schema: "payment_router");
        }
    }
}
