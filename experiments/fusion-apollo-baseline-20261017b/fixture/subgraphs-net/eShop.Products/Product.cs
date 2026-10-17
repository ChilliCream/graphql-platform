using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;

namespace eShop.Products;

[Key("upc")]
public sealed class Product
{
    public required string Upc { get; init; }

    public required string Name { get; init; }

    public int Price { get; init; }

    public int Weight { get; init; }

    [ReferenceResolver]
    public static Product? ResolveReference(string upc)
        => Query.Products.FirstOrDefault(p => p.Upc == upc);
}
