using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class Sagas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "saga_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    saga_name = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<JsonDocument>(type: "json", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                    table.PrimaryKey("PK_saga_states", x => new { x.id, x.saga_name }));

            migrationBuilder.CreateIndex(name: "IX_saga_states_created_at", table: "saga_states", column: "created_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "saga_states");
        }
    }
}
