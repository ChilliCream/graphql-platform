using System.Text;
using BeforeGraphQLDotNet.GraphTypes;
using BeforeGraphQLDotNet.Models;
using BeforeGraphQLDotNet.Services;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;

namespace BeforeGraphQLDotNet.Schema;

public sealed class Query : ObjectGraphType
{
    public Query(BookDataStore store)
    {
        Name = "Query";

        Field<NonNullGraphType<ListGraphType<NonNullGraphType<BookType>>>, IEnumerable<Book>>("books")
            .Argument<BookFilterInputType>("filter")
            .Resolve(context =>
            {
                var books = store.GetBooks().AsEnumerable();
                var filter = context.GetArgument<BookFilter?>("filter");

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
            });

        Field<NonNullGraphType<ListGraphType<NonNullGraphType<AuthorType>>>, IEnumerable<Author>>("authors")
            .Resolve(context => store.GetAuthors());

        Field<BookType, Book?>("bookById")
            .Argument<NonNullGraphType<IdGraphType>>("id")
            .Resolve(context =>
            {
                var id = context.GetArgument<int>("id");
                return store.GetBookById(id);
            });

        Field<NonNullGraphType<ListGraphType<NonNullGraphType<SearchResultUnion>>>, IEnumerable<object>>("search")
            .Argument<NonNullGraphType<StringGraphType>>("term")
            .Resolve(context =>
            {
                var term = context.GetArgument<string>("term");
                var results = new List<object>();

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
            });

        Field<NonNullGraphType<StringGraphType>, string>("secret")
            .Resolve(context => "The cake is a lie.")
            .AuthorizeWithPolicy("Authenticated");

        // Relay cursor (connection) pagination over the seeded books, stable
        // order by id ascending. Custom edge/connection graph types give the
        // schema the "BooksConnection" / "BooksEdge" names.
        Connection<BookType, BooksEdgeType, BooksConnectionType>("booksConnection")
            .Bidirectional()
            .Resolve(context => ResolveBooksConnection(context, store));
    }

    private static Connection<Book> ResolveBooksConnection(
        IResolveConnectionContext<object?> context,
        BookDataStore store)
    {
        var ordered = store.GetBooks().OrderBy(b => b.Id).ToList();

        var afterIndex = -1;
        if (!string.IsNullOrEmpty(context.After) && TryDecodeCursor(context.After, out var afterId))
        {
            afterIndex = ordered.FindIndex(b => b.Id == afterId);
        }

        var beforeIndex = ordered.Count;
        if (!string.IsNullOrEmpty(context.Before) && TryDecodeCursor(context.Before, out var beforeId))
        {
            var found = ordered.FindIndex(b => b.Id == beforeId);
            if (found >= 0)
            {
                beforeIndex = found;
            }
        }

        // Window of candidate items strictly between the after/before cursors.
        var start = afterIndex + 1;
        var end = beforeIndex;
        if (end < start)
        {
            end = start;
        }

        var window = ordered.GetRange(start, end - start);

        if (context.First is int first)
        {
            if (window.Count > first)
            {
                window = window.GetRange(0, first);
            }
        }
        else if (context.Last is int last)
        {
            if (window.Count > last)
            {
                window = window.GetRange(window.Count - last, last);
            }
        }

        var edges = window
            .Select(book => new Edge<Book>
            {
                Cursor = EncodeCursor(book.Id),
                Node = book
            })
            .ToList();

        var firstWindowItem = window.Count > 0 ? window[0] : null;
        var lastWindowItem = window.Count > 0 ? window[^1] : null;

        var hasPreviousPage = firstWindowItem is not null
            && ordered.FindIndex(b => b.Id == firstWindowItem.Id) > 0;
        var hasNextPage = lastWindowItem is not null
            && ordered.FindIndex(b => b.Id == lastWindowItem.Id) < ordered.Count - 1;

        return new Connection<Book>
        {
            Edges = edges,
            TotalCount = ordered.Count,
            PageInfo = new PageInfo
            {
                HasNextPage = hasNextPage,
                HasPreviousPage = hasPreviousPage,
                StartCursor = edges.Count > 0 ? edges[0].Cursor : null,
                EndCursor = edges.Count > 0 ? edges[^1].Cursor : null
            }
        };
    }

    private static string EncodeCursor(int id)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(id.ToString()));
    }

    private static bool TryDecodeCursor(string cursor, out int id)
    {
        id = 0;
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            return int.TryParse(decoded, out id);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
