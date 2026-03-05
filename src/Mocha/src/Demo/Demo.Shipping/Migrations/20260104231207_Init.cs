using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Shipping.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TrackingNumber = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: true),
                    Carrier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ShippedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EstimatedDelivery = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                    table.PrimaryKey("PK_Shipments", x => x.Id));

            migrationBuilder.CreateTable(
                name: "ShipmentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentItems_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentItems_ShipmentId",
                table: "ShipmentItems",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_OrderId",
                table: "Shipments",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(name: "IX_Shipments_Status", table: "Shipments", column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TrackingNumber",
                table: "Shipments",
                column: "TrackingNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ShipmentItems");

            migrationBuilder.DropTable(name: "Shipments");
        }
    }
}
