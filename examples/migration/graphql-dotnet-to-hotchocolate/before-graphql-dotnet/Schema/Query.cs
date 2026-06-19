using BeforeGraphQLDotNet.GraphTypes;
using BeforeGraphQLDotNet.Models;
using BeforeGraphQLDotNet.Services;
using GraphQL;
using GraphQL.Types;

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
    }
}
