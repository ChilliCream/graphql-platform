namespace eShop.Products;

public sealed class Product
{
    public required string Upc { get; init; }

    public required string Name { get; init; }

    public long Price { get; init; }

    public long Weight { get; init; }
}
