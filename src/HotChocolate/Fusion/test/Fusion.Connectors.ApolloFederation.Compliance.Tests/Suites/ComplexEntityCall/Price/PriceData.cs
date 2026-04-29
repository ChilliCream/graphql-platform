namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Price;

/// <summary>
/// Seed data for the <c>price</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/complex-entity-call/data.ts</c>.
/// </summary>
internal static class PriceData
{
    public static readonly IReadOnlyList<Product> Items =
    [
        new Product
        {
            Id = "1",
            Pid = "p1",
            Category = new Category { Id = "c1", Tag = "t1" },
            Price = new Price { Value = 100 }
        },
        new Product
        {
            Id = "2",
            Pid = "p2",
            Category = new Category { Id = "c2", Tag = "t2" },
            Price = new Price { Value = 200 }
        }
    ];

    public static readonly IReadOnlyDictionary<string, Product> ById =
        Items.ToDictionary(static p => p.Id, StringComparer.Ordinal);

    public static Product ResolveByKey(string id, string? pid, string? categoryId, string? categoryTag)
    {
        if (ById.TryGetValue(id, out var exact))
        {
            return exact;
        }

        return new Product
        {
            Id = id,
            Pid = pid,
            Category = categoryId is null
                ? null
                : new Category { Id = categoryId, Tag = categoryTag }
        };
    }
}
