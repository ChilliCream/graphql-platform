namespace HotChocolate.Fusion.Suites.NullKeys.C;

/// <summary>
/// Seed data for the <c>c</c> subgraph.
/// </summary>
internal static class CData
{
    public static readonly IReadOnlyList<Book> Books =
    [
        new Book { Id = "1", Author = new Author { Id = "a1", Name = "Alice" } },
        new Book { Id = "2", Author = new Author { Id = "a2", Name = "Bob" } },
        new Book { Id = "3", Author = new Author { Id = "a3", Name = "Jack" } }
    ];

    public static readonly IReadOnlyDictionary<string, Book> ById =
        Books.ToDictionary(static b => b.Id, StringComparer.Ordinal);
}
