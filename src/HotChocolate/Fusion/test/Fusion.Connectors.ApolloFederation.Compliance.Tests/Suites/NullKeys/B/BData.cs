namespace HotChocolate.Fusion.Suites.NullKeys.B;

/// <summary>
/// Seed data for the <c>b</c> subgraph.
/// </summary>
internal static class BData
{
    public static readonly IReadOnlyList<Book> Books =
    [
        new Book { Id = "1", Upc = "b1" },
        new Book { Id = "2", Upc = "b2" },
        new Book { Id = "3", Upc = "b3" }
    ];
}
