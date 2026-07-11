namespace HotChocolate.Fusion.Suites.ProvidesOnUnion.SubgraphC;

internal static class SubgraphCData
{
    public static readonly IReadOnlyDictionary<string, Book> BooksById =
        new Dictionary<string, Book>(StringComparer.Ordinal)
        {
            ["m1"] = new Book { Id = "m1", Title = "Book 1" }
        };

    public static readonly IReadOnlyDictionary<string, Movie> MoviesById =
        new Dictionary<string, Movie>(StringComparer.Ordinal)
        {
            ["m2"] = new Movie { Id = "m2", Title = "Movie 1" }
        };
}
