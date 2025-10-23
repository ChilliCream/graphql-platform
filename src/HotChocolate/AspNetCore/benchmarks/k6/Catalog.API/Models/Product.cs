namespace eShop.Catalog.Models;

public sealed class Product
{
    public required int Id { get; set; }
    
    public required string Name { get; set; }

    public required string? Description { get; set; }

    public required decimal Price { get; set; }

    public required int TypeId { get; set; }
    
    public required int BrandId { get; set; }
    
    public required int AvailableStock { get; set; }

    public required int RestockThreshold { get; set; }
    
    public required int MaxStockThreshold { get; set; }

    public required bool OnReorder { get; set; }
}
