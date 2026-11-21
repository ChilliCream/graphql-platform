namespace HotChocolate.Fusion.Suites.UnionOverfetching.Card;

internal static class CardData
{
    public static readonly ComponentA ComponentA = new()
    {
        A = "component a",
        Product = new Product { Id = "product-a" }
    };

    public static readonly ComponentB ComponentB = new()
    {
        B = "component b",
        Product = new Product { Id = "product-b" }
    };

    public static readonly ComponentC ComponentC = new()
    {
        C = "component c",
        Product = new Product { Id = "product-c" }
    };

    // The response contains no ComponentC.
    public static readonly ProductCard Card = new()
    {
        Id = "card-1",
        Components = [ComponentA, ComponentB]
    };

    // The response contains only a ComponentC.
    public static readonly ProductCard CardC = new()
    {
        Id = "card-c",
        Components = [ComponentC]
    };
}
