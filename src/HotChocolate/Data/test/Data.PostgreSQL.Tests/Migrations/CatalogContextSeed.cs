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

            context.Brands.RemoveRange(context.Brands);
            await context.Brands.AddRangeAsync(
                sourceItems.Select(x => x.Brand).Distinct().Select(brandName => new Brand { Name = brandName }));

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
