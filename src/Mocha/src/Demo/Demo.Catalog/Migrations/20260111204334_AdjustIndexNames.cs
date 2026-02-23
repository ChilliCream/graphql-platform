using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class AdjustIndexNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(name: "PK_saga_states", table: "saga_states");

            migrationBuilder.DropPrimaryKey(name: "PK_outbox_messages", table: "outbox_messages");

            migrationBuilder.RenameIndex(
                name: "IX_saga_states_created_at",
                newName: "ix_saga_states_created_at",
                table: "saga_states");

            migrationBuilder.RenameIndex(
                name: "IX_outbox_messages_times_sent",
                newName: "ix_outbox_messages_times_sent",
                table: "outbox_messages");

            migrationBuilder.RenameIndex(
                name: "IX_outbox_messages_created_at",
                newName: "ix_outbox_messages_created_at",
                table: "outbox_messages");

            migrationBuilder.AddPrimaryKey(
                name: "ix_saga_states_primary_key",
                table: "saga_states",
                columns: new[] { "id", "saga_name" });

            migrationBuilder.AddPrimaryKey(
                name: "ix_outbox_messages_primary_key",
                table: "outbox_messages",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(name: "ix_saga_states_primary_key", table: "saga_states");

            migrationBuilder.DropPrimaryKey(name: "ix_outbox_messages_primary_key", table: "outbox_messages");

            migrationBuilder.RenameIndex(
                name: "ix_saga_states_created_at",
                newName: "IX_saga_states_created_at",
                table: "saga_states");

            migrationBuilder.RenameIndex(
                name: "ix_outbox_messages_times_sent",
                newName: "IX_outbox_messages_times_sent",
                table: "outbox_messages");

            migrationBuilder.RenameIndex(
                name: "ix_outbox_messages_created_at",
                newName: "IX_outbox_messages_created_at",
                table: "outbox_messages");

            migrationBuilder.AddPrimaryKey(
                name: "PK_saga_states",
                table: "saga_states",
                columns: new[] { "id", "saga_name" });

            migrationBuilder.AddPrimaryKey(name: "PK_outbox_messages", table: "outbox_messages", column: "id");
        }
    }
}
