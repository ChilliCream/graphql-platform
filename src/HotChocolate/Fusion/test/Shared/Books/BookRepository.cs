namespace HotChocolate.Fusion.Shared.Books;

public sealed class BookRepository
{
    private readonly Dictionary<string, Book> _books;

    public BookRepository()
    {
        _books = new[]
        {
            new Book("1", "1", "The first book")
        }.ToDictionary(t => t.Id);
    }

     public IEnumerable<Book> GetBooks(int limit)
        => _books.Values.OrderBy(t => t.Id).Take(limit);

    public Book? GetBookById(string id)
        => _books.TryGetValue(id, out var book)
            ? book
            : null;

     public IEnumerable<Book> GetBooksByAuthorId(string authorId)
        => _books.Values.Where(b => b.AuthorId.Equals(authorId));
}
