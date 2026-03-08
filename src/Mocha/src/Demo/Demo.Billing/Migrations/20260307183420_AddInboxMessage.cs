using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Billing.Migrations
{
    /// <inheritdoc />
    public partial class AddInboxMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "RevenueSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderCount = table.Column<int>(type: "integer", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false, precision: 18, scale: 2),
                    AverageOrderAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, precision: 18, scale: 2),
                    TotalItemsSold = table.Column<int>(type: "integer", nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletionMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                    table.PrimaryKey("PK_RevenueSummaries", x => x.Id));

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

            migrationBuilder.DropTable(
                name: "RevenueSummaries");
        }
    }
}
