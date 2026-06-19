using AfterHotChocolate.Models;
using AfterHotChocolate.Services;
using HotChocolate.Authorization;

namespace AfterHotChocolate.GraphQL;

[QueryType]
public static partial class Query
{
    public static IEnumerable<Book> GetBooks(BookFilter? filter, BookDataStore store)
    {
        var books = store.GetBooks().AsEnumerable();

        if (filter is not null)
        {
            if (filter.Genre is not null)
            {
                books = books.Where(b => b.Genre == filter.Genre.Value);
            }

            if (!string.IsNullOrEmpty(filter.TitleContains))
            {
                books = books.Where(b =>
                    b.Title.Contains(filter.TitleContains, StringComparison.OrdinalIgnoreCase));
            }
        }

        return books.ToList();
    }

    public static IReadOnlyList<Author> GetAuthors(BookDataStore store)
        => store.GetAuthors();

    // With [UsePaging] the connection is named after the field (booksConnection).
    // Stable order by id so cursors are deterministic across pages.
    [UsePaging]
    public static IEnumerable<Book> GetBooksConnection(BookDataStore store)
        => store.GetBooks().OrderBy(b => b.Id);

    public static Book? GetBookById([ID] int id, BookDataStore store)
        => store.GetBookById(id);

    public static IEnumerable<ISearchResult> Search(string term, BookDataStore store)
    {
        var results = new List<ISearchResult>();

        foreach (var book in store.GetBooks())
        {
            if (book.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(book);
            }
        }

        foreach (var author in store.GetAuthors())
        {
            if (author.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(author);
            }
        }

        return results;
    }

    [Authorize(Policy = "Authenticated")]
    public static string GetSecret()
        => "The cake is a lie.";
}
