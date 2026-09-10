namespace HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.B;

internal static class BData
{
    public static readonly IReadOnlyList<Bread> Products =
    [
        new Bread { Id = "p1", Name = "Bread" }
    ];

    public static readonly IReadOnlyDictionary<string, Bread> ProductsById =
        Products.ToDictionary(static p => p.Id, StringComparer.Ordinal);
}
