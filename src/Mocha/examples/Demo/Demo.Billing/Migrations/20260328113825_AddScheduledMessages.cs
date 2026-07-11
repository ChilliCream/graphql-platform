using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotChocolate.Demo.Billing.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scheduled_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    envelope = table.Column<JsonDocument>(type: "json", nullable: false),
                    scheduled_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    times_sent = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ix_scheduled_messages_primary_key", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_messages_scheduled_time",
                table: "scheduled_messages",
                column: "scheduled_time",
                filter: "\"times_sent\" < \"max_attempts\"");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_messages_times_sent",
                table: "scheduled_messages",
                column: "times_sent");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scheduled_messages");
        }
    }
}
