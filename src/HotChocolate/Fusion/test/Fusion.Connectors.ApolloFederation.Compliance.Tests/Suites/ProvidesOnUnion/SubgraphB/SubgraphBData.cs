namespace HotChocolate.Fusion.Suites.ProvidesOnUnion.SubgraphB;

internal static class SubgraphBData
{
    public static readonly IReadOnlyDictionary<string, Book> BooksById =
        new Dictionary<string, Book>(StringComparer.Ordinal)
        {
            ["m1"] = new Book { Id = "m1", Title = "Book 1" }
        };

    public static readonly List<object> Media =
    [
        new Book { Id = "m1", Title = "Book 1" },
        new Movie { Id = "m2" }
    ];
}
