namespace eShop.Inventory;

public sealed class Product
{
    public required string Upc { get; init; }

    public bool InStock { get; init; }
}
