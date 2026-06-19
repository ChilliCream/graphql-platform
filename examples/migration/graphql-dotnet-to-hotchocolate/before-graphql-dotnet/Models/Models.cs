namespace BeforeGraphQLDotNet.Models;

public enum BookGenre
{
    Fiction,
    Nonfiction,
    Fantasy,
    Science
}

public sealed class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public BookGenre Genre { get; set; }

    public int PublishedYear { get; set; }

    public int AuthorId { get; set; }
}

public sealed class Author
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public sealed class BookFilter
{
    public BookGenre? Genre { get; set; }

    public string? TitleContains { get; set; }
}
