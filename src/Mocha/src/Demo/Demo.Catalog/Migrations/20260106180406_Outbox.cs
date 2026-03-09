using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class Outbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    envelope = table.Column<JsonDocument>(type: "json", nullable: false),
                    times_sent = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                    table.PrimaryKey("PK_outbox_messages", x => x.id));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    new DateTimeOffset(
                        new DateTime(2026, 1, 6, 18, 4, 6, 412, DateTimeKind.Unspecified).AddTicks(7989),
                        new TimeSpan(0, 0, 0, 0, 0)),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 6, 18, 4, 6, 412, DateTimeKind.Unspecified).AddTicks(8169),
                        new TimeSpan(0, 0, 0, 0, 0))
                });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    new DateTimeOffset(
                        new DateTime(2026, 1, 6, 18, 4, 6, 412, DateTimeKind.Unspecified).AddTicks(8241),
                        new TimeSpan(0, 0, 0, 0, 0)),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 6, 18, 4, 6, 412, DateTimeKind.Unspecified).AddTicks(8241),
                        new TimeSpan(0, 0, 0, 0, 0))
                });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    new DateTimeOffset(
                        new DateTime(2026, 1, 6, 18, 4, 6, 412, DateTimeKind.Unspecified).AddTicks(8244),
                        new TimeSpan(0, 0, 0, 0, 0)),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 6, 18, 4, 6, 412, DateTimeKind.Unspecified).AddTicks(8244),
                        new TimeSpan(0, 0, 0, 0, 0))
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_created_at",
                table: "outbox_messages",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_times_sent",
                table: "outbox_messages",
                column: "times_sent");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "outbox_messages");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    new DateTimeOffset(
                        new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Unspecified).AddTicks(2355),
                        new TimeSpan(0, 0, 0, 0, 0)),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Unspecified).AddTicks(2527),
                        new TimeSpan(0, 0, 0, 0, 0))
                });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    new DateTimeOffset(
                        new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Unspecified).AddTicks(2622),
                        new TimeSpan(0, 0, 0, 0, 0)),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Unspecified).AddTicks(2623),
                        new TimeSpan(0, 0, 0, 0, 0))
                });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    new DateTimeOffset(
                        new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Unspecified).AddTicks(2628),
                        new TimeSpan(0, 0, 0, 0, 0)),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Unspecified).AddTicks(2629),
                        new TimeSpan(0, 0, 0, 0, 0))
                });
        }
    }
}
