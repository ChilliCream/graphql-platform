namespace HotChocolate.Fusion.Suites.NullKeys.A;

/// <summary>
/// Seed data for the <c>a</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/null-keys/data.ts</c>.
/// </summary>
internal static class AData
{
    public static readonly IReadOnlyList<Book> Books =
    [
        new Book { Upc = "b1" },
        new Book { Upc = "b2" },
        new Book { Upc = "b3" }
    ];

    public static readonly IReadOnlyDictionary<string, Book> ByUpc =
        Books.ToDictionary(static b => b.Upc, StringComparer.Ordinal);
}
