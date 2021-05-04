using Microsoft.EntityFrameworkCore.Migrations;

namespace ConferencePlanner.GraphQL.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Speakers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 200, nullable: false),
                    Bio = table.Column<string>(maxLength: 4000, nullable: true),
                    WebSite = table.Column<string>(maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Speakers", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Speakers");
        }
    }
}
