using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class OutboxJsonDoc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    new DateTimeOffset(
                        new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Unspecified),
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
                        new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Unspecified),
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
                        new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0))
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
