// ReSharper disable CollectionNeverUpdated.Global

using System.ComponentModel.DataAnnotations;

namespace eShop.Catalog.Data.Entities;

public sealed class ProductEntity
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = default!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string? ImageFileName { get; set; }

    public int TypeId { get; set; }

    public ProductTypeEntity? Type { get; set; }

    public int BrandId { get; set; }

    public BrandEntity? Brand { get; set; }

    public int AvailableStock { get; set; }

    public int RestockThreshold { get; set; }

    public int MaxStockThreshold { get; set; }

    public bool OnReorder { get; set; }
}
