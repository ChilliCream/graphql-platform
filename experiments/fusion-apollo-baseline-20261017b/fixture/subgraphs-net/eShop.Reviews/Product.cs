using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;

namespace eShop.Reviews;

[ExtendServiceType]
[Key("upc")]
public sealed class Product
{
    internal static readonly Dictionary<string, Product> Products =
        new(StringComparer.Ordinal)
    {
        ["1"] = new() { Upc = "1" },
        ["2"] = new() { Upc = "2" },
        ["3"] = new() { Upc = "3" },
        ["4"] = new() { Upc = "4" },
        ["5"] = new() { Upc = "5" },
        ["6"] = new() { Upc = "6" },
        ["7"] = new() { Upc = "7" },
        ["8"] = new() { Upc = "8" },
        ["9"] = new() { Upc = "9" }
    };

    [External]
    public string Upc { get; set; } = default!;

    public IEnumerable<Review> GetReviews()
        => ReviewRepository.GetByProductUpc(Upc);

    [ReferenceResolver]
    public static Product ResolveReference(string upc)
        => Products.TryGetValue(upc, out var product) ? product : new() { Upc = upc };
}
