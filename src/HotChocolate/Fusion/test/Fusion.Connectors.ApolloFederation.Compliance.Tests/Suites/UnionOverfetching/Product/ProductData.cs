namespace HotChocolate.Fusion.Suites.UnionOverfetching.Product;

internal static class ProductData
{
    public static readonly Product ProductA = new() { Id = "product-a", Kind = "a", A = "A1" };
    public static readonly Product ProductB = new() { Id = "product-b", Kind = "b", B = "B1" };
    public static readonly Product ProductC = new() { Id = "product-c", Kind = "c", C = "C1" };

    public static readonly IReadOnlyList<Product> Products = [ProductA, ProductB, ProductC];
}
