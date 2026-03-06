using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class ChangeAppManagedCOncurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "version",
                table: "saga_states",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<uint>(
                name: "version",
                table: "saga_states",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
