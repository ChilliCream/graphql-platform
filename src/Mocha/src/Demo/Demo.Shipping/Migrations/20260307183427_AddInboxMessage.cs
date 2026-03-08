using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Shipping.Migrations
{
    /// <inheritdoc />
    public partial class AddInboxMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "ReturnShipments",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "ReturnShipments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "ReturnShipments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "ReturnShipments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                columns: table => new
                {
                    message_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    message_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                    table.PrimaryKey("ix_inbox_messages_primary_key", x => x.message_id));

            migrationBuilder.CreateIndex(
                name: "ix_inbox_messages_processed_at",
                table: "inbox_messages",
                column: "processed_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "ReturnShipments");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "ReturnShipments");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "ReturnShipments");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "ReturnShipments");
        }
    }
}
