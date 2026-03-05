using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Demo.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                    table.PrimaryKey("PK_Categories", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(
                        type: "character varying(2000)",
                        maxLength: 2000,
                        nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false, precision: 18, scale: 2),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ShippingAddress = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false),
                    TotalAmount = table.Column<decimal>(
                        type: "numeric(18,2)",
                        nullable: false,
                        precision: 18,
                        scale: 2),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    {
                        new Guid("11111111-1111-1111-1111-111111111111"),
                        "Electronic devices and accessories",
                        "Electronics"
                    },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Physical and digital books", "Books" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[]
                {
                    "Id",
                    "CategoryId",
                    "CreatedAt",
                    "Description",
                    "Name",
                    "Price",
                    "StockQuantity",
                    "UpdatedAt"
                },
                values: new object[,]
                {
                    {
                        new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                        new Guid("11111111-1111-1111-1111-111111111111"),
                        new DateTimeOffset(
                            new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Unspecified).AddTicks(2355),
                            new TimeSpan(0, 0, 0, 0, 0)),
                        "Premium noise-cancelling wireless headphones",
                        "Wireless Headphones",
                        299.99m,
                        50,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Unspecified).AddTicks(2527),
                            new TimeSpan(0, 0, 0, 0, 0))
                    },
                    {
                        new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                        new Guid("11111111-1111-1111-1111-111111111111"),
                        new DateTimeOffset(
                            new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Unspecified).AddTicks(2622),
                            new TimeSpan(0, 0, 0, 0, 0)),
                        "RGB mechanical gaming keyboard",
                        "Mechanical Keyboard",
                        149.99m,
                        100,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Unspecified).AddTicks(2623),
                            new TimeSpan(0, 0, 0, 0, 0))
                    },
                    {
                        new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                        new Guid("22222222-2222-2222-2222-222222222222"),
                        new DateTimeOffset(
                            new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Unspecified).AddTicks(2628),
                            new TimeSpan(0, 0, 0, 0, 0)),
                        "A Handbook of Agile Software Craftsmanship by Robert C. Martin",
                        "Clean Code",
                        39.99m,
                        200,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Unspecified).AddTicks(2629),
                            new TimeSpan(0, 0, 0, 0, 0))
                    }
                });

            migrationBuilder.CreateIndex(name: "IX_Orders_CustomerId", table: "Orders", column: "CustomerId");

            migrationBuilder.CreateIndex(name: "IX_Orders_ProductId", table: "Orders", column: "ProductId");

            migrationBuilder.CreateIndex(name: "IX_Orders_Status", table: "Orders", column: "Status");

            migrationBuilder.CreateIndex(name: "IX_Products_CategoryId", table: "Products", column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Orders");

            migrationBuilder.DropTable(name: "Products");

            migrationBuilder.DropTable(name: "Categories");
        }
    }
}
