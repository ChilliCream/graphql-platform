using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Billing.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundSaga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(name: "PK_outbox_messages", table: "outbox_messages");

            migrationBuilder.RenameIndex(
                name: "IX_outbox_messages_times_sent",
                newName: "ix_outbox_messages_times_sent",
                table: "outbox_messages");

            migrationBuilder.RenameIndex(
                name: "IX_outbox_messages_created_at",
                newName: "ix_outbox_messages_created_at",
                table: "outbox_messages");

            migrationBuilder.AddPrimaryKey(
                name: "ix_outbox_messages_primary_key",
                table: "outbox_messages",
                column: "id");

            migrationBuilder.CreateTable(
                name: "Refunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginalAmount = table.Column<decimal>(
                        type: "numeric(18,2)",
                        nullable: false,
                        precision: 18,
                        scale: 2),
                    RefundedAmount = table.Column<decimal>(
                        type: "numeric(18,2)",
                        nullable: false,
                        precision: 18,
                        scale: 2),
                    RefundPercentage = table.Column<decimal>(
                        type: "numeric(5,2)",
                        nullable: false,
                        precision: 5,
                        scale: 2),
                    CustomerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Refunds_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(name: "IX_Refunds_InvoiceId", table: "Refunds", column: "InvoiceId");

            migrationBuilder.CreateIndex(name: "IX_Refunds_OrderId", table: "Refunds", column: "OrderId");

            migrationBuilder.CreateIndex(name: "IX_Refunds_Status", table: "Refunds", column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Refunds");

            migrationBuilder.DropPrimaryKey(name: "ix_outbox_messages_primary_key", table: "outbox_messages");

            migrationBuilder.RenameIndex(
                name: "ix_outbox_messages_times_sent",
                newName: "IX_outbox_messages_times_sent",
                table: "outbox_messages");

            migrationBuilder.RenameIndex(
                name: "ix_outbox_messages_created_at",
                newName: "IX_outbox_messages_created_at",
                table: "outbox_messages");

            migrationBuilder.AddPrimaryKey(name: "PK_outbox_messages", table: "outbox_messages", column: "id");
        }
    }
}
