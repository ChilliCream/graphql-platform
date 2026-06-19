using HotChocolate;

namespace AfterHotChocolate.Models;

public enum BookGenre
{
    Fiction,
    Nonfiction,
    Fantasy,
    Science
}

// Marker interface that makes Book and Author members of the SearchResult union.
[UnionType("SearchResult")]
public interface ISearchResult;

public sealed class Book : ISearchResult
{
    [ID]
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public BookGenre Genre { get; set; }

    public int PublishedYear { get; set; }

    // Backing foreign key used by the author DataLoader; not exposed in the schema.
    [GraphQLIgnore]
    public int AuthorId { get; set; }
}

public sealed class Author : ISearchResult
{
    [ID]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public sealed class BookFilter
{
    public BookGenre? Genre { get; set; }

    public string? TitleContains { get; set; }
}
