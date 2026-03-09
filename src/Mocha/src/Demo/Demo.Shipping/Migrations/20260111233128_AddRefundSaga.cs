using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Shipping.Migrations
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
                name: "ReturnShipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerAddress = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TrackingNumber = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: true),
                    LabelUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnShipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnShipments_Shipments_OriginalShipmentId",
                        column: x => x.OriginalShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnShipments_OrderId",
                table: "ReturnShipments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnShipments_OriginalShipmentId",
                table: "ReturnShipments",
                column: "OriginalShipmentId");

            migrationBuilder.CreateIndex(name: "IX_ReturnShipments_Status", table: "ReturnShipments", column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnShipments_TrackingNumber",
                table: "ReturnShipments",
                column: "TrackingNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ReturnShipments");

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
