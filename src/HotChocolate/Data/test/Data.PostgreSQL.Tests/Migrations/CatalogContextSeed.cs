using System.Text.Json;
using HotChocolate.Data.Data;
using HotChocolate.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.Migrations;

public sealed class CatalogContextSeed : IDbSeeder<CatalogContext>
{
    public async Task SeedAsync(CatalogContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (!context.Products.Any())
        {
            var sourceJson = FileResource.Open("catalog.json");
            var sourceItems = JsonSerializer.Deserialize<ProductEntry[]>(sourceJson)!;

            // Seed suppliers first (brands will reference them).
            context.Suppliers.RemoveRange(context.Suppliers);
            var suppliers = new[]
            {
                new Supplier { Name = "Global Supply Co.", Website = "https://globalsupply.example.com", ContactEmail = "info@globalsupply.example.com" },
                new Supplier { Name = "Prime Distribution", Website = "https://primedist.example.com", ContactEmail = "sales@primedist.example.com" },
                new Supplier { Name = "Atlas Logistics", Website = "https://atlaslogistics.example.com", ContactEmail = "contact@atlaslogistics.example.com" }
            };
            await context.Suppliers.AddRangeAsync(suppliers);
            await context.SaveChangesAsync();

            var supplierIds = await context.Suppliers.Select(s => s.Id).ToListAsync();

            context.Brands.RemoveRange(context.Brands);
            var brandNames = sourceItems.Select(x => x.Brand).Distinct().ToList();
            await context.Brands.AddRangeAsync(
                brandNames.Select((brandName, i) => new Brand
                {
                    Name = brandName,
                    SupplierId = supplierIds[i % supplierIds.Count]
                }));

            context.ProductTypes.RemoveRange(context.ProductTypes);
            await context.ProductTypes.AddRangeAsync(
                sourceItems.Select(x => x.Type).Distinct().Select(typeName => new ProductType { Name = typeName }));

            await context.SaveChangesAsync();

            var brandIdsByName = await context.Brands.ToDictionaryAsync(x => x.Name, x => x.Id);
            var typeIdsByName = await context.ProductTypes.ToDictionaryAsync(x => x.Name, x => x.Id);

            await context.Products.AddRangeAsync(
                sourceItems.Select(source => new Product
                {
                    Id = source.Id,
                    Name = source.Name,
                    Description = source.Description,
                    Price = source.Price,
                    BrandId = brandIdsByName[source.Brand],
                    TypeId = typeIdsByName[source.Type],
                    AvailableStock = 100,
                    MaxStockThreshold = 200,
                    RestockThreshold = 10,
                    ImageFileName = $"images/{source.Id}.webp"
                }));

            for (var i = 0; i < 100; i++)
            {
                await context.SingleProperties.AddAsync(
                    new SingleProperty
                    {
                        Id = i.ToString()
                    });
            }

            await context.SaveChangesAsync();
        }
    }

    private sealed class ProductEntry
    {
        public required int Id { get; set; }
        public required string Type { get; set; }
        public required string Brand { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required decimal Price { get; set; }
    }
}
