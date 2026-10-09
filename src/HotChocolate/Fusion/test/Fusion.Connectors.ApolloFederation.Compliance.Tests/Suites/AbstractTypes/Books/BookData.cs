namespace HotChocolate.Fusion.Suites.AbstractTypes.Books;

public static class BookData
{
    public static readonly IReadOnlyList<Book> Books =
    [
        new Book { Id = "p1", Title = "Book 1" },
        new Book { Id = "p3", Title = "Book 2" }
    ];

    public static readonly IReadOnlyDictionary<string, Book> BooksById =
        Books.ToDictionary(static b => b.Id, StringComparer.Ordinal);
}

public sealed class Book
{
    public string Id { get; init; } = default!;
    public string? Title { get; init; }
}
