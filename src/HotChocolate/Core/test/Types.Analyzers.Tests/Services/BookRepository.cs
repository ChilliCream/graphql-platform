namespace HotChocolate.Types;

public sealed class BookRepository
{
    private readonly Dictionary<int, Book> _books = new()
    {
        { 1, new Book(1, "Book 1", 1, Genre.Fiction) },
        { 2, new Book(2, "Book 2", 2, Genre.NonFiction) },
        { 3, new Book(3, "Book 3", 3, Genre.NonFiction) }
    };

    public Task<Book?> GetBookAsync(int id, CancellationToken cancellationToken)
        => _books.TryGetValue(id, out var book)
            ? Task.FromResult<Book?>(book)
            : Task.FromResult<Book?>(null);

    public Task<IEnumerable<Book>> GetBooksAsync(CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<Book>>(_books.Values.OrderBy(t => t.Id));

    public Task<IEnumerable<Book>> GetBooksByAuthorAsync(int authorId, CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<Book>>(_books.Values.Where(t => t.AuthorId == authorId).OrderBy(t => t.Id));

    public Task<Book> CreateBookAsync(string title, Genre genre, int authorId, CancellationToken cancellationToken)
    {
        var book = new Book(_books.Count + 1, title, authorId, genre);
        _books.Add(book.Id, book);
        return Task.FromResult(book);
    }
}
