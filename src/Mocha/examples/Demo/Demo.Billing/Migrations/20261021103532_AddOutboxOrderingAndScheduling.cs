using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotChocolate.Demo.Billing.Migrations;

/// <inheritdoc />
public partial class AddOutboxOrderingAndScheduling : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "group_id",
            table: "outbox_messages",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "scheduled_at",
            table: "outbox_messages",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "sequence",
            table: "outbox_messages",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.CreateIndex(
            name: "ix_outbox_messages_group_sequence",
            table: "outbox_messages",
            columns: new[] { "group_id", "sequence" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_outbox_messages_group_sequence",
            table: "outbox_messages");

        migrationBuilder.DropColumn(
            name: "group_id",
            table: "outbox_messages");

        migrationBuilder.DropColumn(
            name: "scheduled_at",
            table: "outbox_messages");

        migrationBuilder.DropColumn(
            name: "sequence",
            table: "outbox_messages");
    }
}
