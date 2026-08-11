using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiskAssessment.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "risk_assessment");

            migrationBuilder.CreateTable(
                name: "blacklist_entries",
                schema: "risk_assessment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    reason = table.Column<string>(type: "TEXT", nullable: true),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    added_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blacklist_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "merchant_risk_profiles",
                schema: "risk_assessment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    merchant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    risk_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    auto_reject_threshold = table.Column<int>(type: "integer", nullable: false, defaultValue: 80),
                    manual_review_threshold = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    max_transaction_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    max_transaction_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_merchant_risk_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "risk_evaluations",
                schema: "risk_assessment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payment_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    merchant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    risk_score = table.Column<int>(type: "integer", nullable: false),
                    decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    rule_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    evaluated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    customer_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_risk_evaluations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "risk_rules",
                schema: "risk_assessment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    rule_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: false),
                    rule_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    score_impact = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    conditions_json = table.Column<string>(type: "JSONB", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_risk_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "whitelist_entries",
                schema: "risk_assessment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    reason = table.Column<string>(type: "TEXT", nullable: true),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    added_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_whitelist_entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_blacklist_entity_type",
                schema: "risk_assessment",
                table: "blacklist_entries",
                column: "entity_type");

            migrationBuilder.CreateIndex(
                name: "idx_blacklist_entity_value",
                schema: "risk_assessment",
                table: "blacklist_entries",
                column: "entity_value");

            migrationBuilder.CreateIndex(
                name: "uq_blacklist_entry",
                schema: "risk_assessment",
                table: "blacklist_entries",
                columns: new[] { "entity_type", "entity_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_merchant_id",
                schema: "risk_assessment",
                table: "merchant_risk_profiles",
                column: "merchant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_customer_id",
                schema: "risk_assessment",
                table: "risk_evaluations",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "idx_evaluated_at",
                schema: "risk_assessment",
                table: "risk_evaluations",
                column: "evaluated_at");

            migrationBuilder.CreateIndex(
                name: "idx_merchant_id",
                schema: "risk_assessment",
                table: "risk_evaluations",
                column: "merchant_id");

            migrationBuilder.CreateIndex(
                name: "idx_payment_id",
                schema: "risk_assessment",
                table: "risk_evaluations",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "idx_risk_decision",
                schema: "risk_assessment",
                table: "risk_evaluations",
                column: "decision");

            migrationBuilder.CreateIndex(
                name: "ix_risk_evaluations_evaluation_id",
                schema: "risk_assessment",
                table: "risk_evaluations",
                column: "evaluation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_is_active",
                schema: "risk_assessment",
                table: "risk_rules",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_priority",
                schema: "risk_assessment",
                table: "risk_rules",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "idx_rule_id",
                schema: "risk_assessment",
                table: "risk_rules",
                column: "rule_id");

            migrationBuilder.CreateIndex(
                name: "idx_rule_type",
                schema: "risk_assessment",
                table: "risk_rules",
                column: "rule_type");

            migrationBuilder.CreateIndex(
                name: "uq_rule_version",
                schema: "risk_assessment",
                table: "risk_rules",
                columns: new[] { "rule_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_whitelist_entity_type",
                schema: "risk_assessment",
                table: "whitelist_entries",
                column: "entity_type");

            migrationBuilder.CreateIndex(
                name: "idx_whitelist_entity_value",
                schema: "risk_assessment",
                table: "whitelist_entries",
                column: "entity_value");

            migrationBuilder.CreateIndex(
                name: "uq_whitelist_entry",
                schema: "risk_assessment",
                table: "whitelist_entries",
                columns: new[] { "entity_type", "entity_value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blacklist_entries",
                schema: "risk_assessment");

            migrationBuilder.DropTable(
                name: "merchant_risk_profiles",
                schema: "risk_assessment");

            migrationBuilder.DropTable(
                name: "risk_evaluations",
                schema: "risk_assessment");

            migrationBuilder.DropTable(
                name: "risk_rules",
                schema: "risk_assessment");

            migrationBuilder.DropTable(
                name: "whitelist_entries",
                schema: "risk_assessment");
        }
    }
}
