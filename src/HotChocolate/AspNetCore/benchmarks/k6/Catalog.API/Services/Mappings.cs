using eShop.Catalog.Data.Entities;

namespace eShop.Catalog.Services;

public static class Mappings
{
    public static IQueryable<Product> MapToProduct(
        this IQueryable<ProductEntity> queryable)
        => queryable.Select(t => new Product
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            Price = t.Price,
            TypeId = t.TypeId,
            BrandId = t.BrandId,
            AvailableStock = t.AvailableStock,
            RestockThreshold = t.RestockThreshold,
            MaxStockThreshold = t.MaxStockThreshold,
            OnReorder = t.OnReorder
        });

    public static IQueryable<Brand> MapToBrand(
        this IQueryable<BrandEntity> queryable)
        => queryable.Select(b => new Brand { Id = b.Id, Name = b.Name });

    public static IQueryable<ProductType> MapToProductType(
        this IQueryable<ProductTypeEntity> queryable)
        => queryable.Select(b => new ProductType { Id = b.Id, Name = b.Name });
}
