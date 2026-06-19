using AfterHotChocolate.Models;

namespace AfterHotChocolate.Services;

// Singleton in-memory data store with deterministic seed data.
public sealed class BookDataStore
{
    private readonly object _gate = new();
    private readonly List<Author> _authors;
    private readonly List<Book> _books;

    public BookDataStore()
    {
        _authors =
        [
            new() { Id = 1, Name = "George Orwell" },
            new() { Id = 2, Name = "J.R.R. Tolkien" },
            new() { Id = 3, Name = "Carl Sagan" }
        ];

        _books =
        [
            new() { Id = 1, Title = "1984", Genre = BookGenre.Fiction, PublishedYear = 1949, AuthorId = 1 },
            new() { Id = 2, Title = "Animal Farm", Genre = BookGenre.Fiction, PublishedYear = 1945, AuthorId = 1 },
            new() { Id = 3, Title = "The Hobbit", Genre = BookGenre.Fantasy, PublishedYear = 1937, AuthorId = 2 },
            new() { Id = 4, Title = "The Lord of the Rings", Genre = BookGenre.Fantasy, PublishedYear = 1954, AuthorId = 2 },
            new() { Id = 5, Title = "Cosmos", Genre = BookGenre.Nonfiction, PublishedYear = 1980, AuthorId = 3 },
            new() { Id = 6, Title = "The Demon-Haunted World", Genre = BookGenre.Science, PublishedYear = 1995, AuthorId = 3 }
        ];
    }

    public IReadOnlyList<Book> GetBooks()
    {
        lock (_gate)
        {
            return _books.ToList();
        }
    }

    public IReadOnlyList<Author> GetAuthors()
    {
        lock (_gate)
        {
            return _authors.ToList();
        }
    }

    public Book? GetBookById(int id)
    {
        lock (_gate)
        {
            return _books.FirstOrDefault(b => b.Id == id);
        }
    }

    public IReadOnlyList<Book> GetBooksByAuthorId(int authorId)
    {
        lock (_gate)
        {
            return _books.Where(b => b.AuthorId == authorId).ToList();
        }
    }

    public IReadOnlyList<Author> GetAuthorsByIds(IEnumerable<int> ids)
    {
        lock (_gate)
        {
            var idSet = ids.ToHashSet();
            return _authors.Where(a => idSet.Contains(a.Id)).ToList();
        }
    }

    public Book AddBook(string title, int authorId, BookGenre genre, int publishedYear)
    {
        lock (_gate)
        {
            var nextId = _books.Count == 0 ? 1 : _books.Max(b => b.Id) + 1;
            var book = new Book
            {
                Id = nextId,
                Title = title,
                AuthorId = authorId,
                Genre = genre,
                PublishedYear = publishedYear
            };

            _books.Add(book);
            return book;
        }
    }
}
