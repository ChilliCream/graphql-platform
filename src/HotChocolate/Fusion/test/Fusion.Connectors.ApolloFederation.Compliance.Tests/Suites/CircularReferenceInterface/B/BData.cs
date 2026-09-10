namespace HotChocolate.Fusion.Suites.CircularReferenceInterface.B;

internal static class BData
{
    public static readonly IReadOnlyList<Book> Books =
    [
        new Book { Id = "1", Price = 10.99 },
        new Book { Id = "2", Price = 5.99 },
        new Book { Id = "3", Price = 10.99 }
    ];

    public static readonly IReadOnlyDictionary<string, Book> BooksById =
        Books.ToDictionary(static b => b.Id, StringComparer.Ordinal);
}
