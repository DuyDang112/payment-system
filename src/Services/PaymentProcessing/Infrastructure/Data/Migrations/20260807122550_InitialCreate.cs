using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentProcessing.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payment_processing");

            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                schema: "payment_processing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    MerchantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_keys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payment_attempts",
                schema: "payment_processing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_attempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "payment_processing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MerchantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PaymentMethodToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "state_transitions",
                schema: "payment_processing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<int>(type: "integer", nullable: false),
                    to_status = table.Column<int>(type: "integer", nullable: false),
                    TransitionedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_state_transitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_keys_ExpiresAt",
                schema: "payment_processing",
                table: "idempotency_keys",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_keys_MerchantId_Key",
                schema: "payment_processing",
                table: "idempotency_keys",
                columns: new[] { "MerchantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_attempts_PaymentId",
                schema: "payment_processing",
                table: "payment_attempts",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_IdempotencyKey",
                schema: "payment_processing",
                table: "payments",
                column: "IdempotencyKey");

            migrationBuilder.CreateIndex(
                name: "IX_payments_MerchantId",
                schema: "payment_processing",
                table: "payments",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_PaymentId",
                schema: "payment_processing",
                table: "payments",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_state_transitions_PaymentId",
                schema: "payment_processing",
                table: "state_transitions",
                column: "PaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: "payment_processing");

            migrationBuilder.DropTable(
                name: "payment_attempts",
                schema: "payment_processing");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "payment_processing");

            migrationBuilder.DropTable(
                name: "state_transitions",
                schema: "payment_processing");
        }
    }
}
