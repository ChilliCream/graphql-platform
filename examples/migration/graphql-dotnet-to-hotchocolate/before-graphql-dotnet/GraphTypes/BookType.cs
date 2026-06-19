using BeforeGraphQLDotNet.Models;
using BeforeGraphQLDotNet.Services;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;

namespace BeforeGraphQLDotNet.GraphTypes;

public sealed class BookType : ObjectGraphType<Book>
{
    public BookType(IDataLoaderContextAccessor accessor, BookDataStore store)
    {
        Name = "Book";

        Field<NonNullGraphType<IdGraphType>>("id")
            .Resolve(context => context.Source.Id);

        Field<NonNullGraphType<StringGraphType>>("title")
            .Resolve(context => context.Source.Title);

        Field<NonNullGraphType<BookGenreEnum>>("genre")
            .Resolve(context => context.Source.Genre);

        Field<NonNullGraphType<IntGraphType>>("publishedYear")
            .Resolve(context => context.Source.PublishedYear);

        // Author resolved through a batch DataLoader to demonstrate the N+1 fix.
        Field<NonNullGraphType<AuthorType>, Author>("author")
            .ResolveAsync(context =>
            {
                var loader = accessor.Context!.GetOrAddBatchLoader<int, Author>(
                    "GetAuthorsByIds",
                    async authorIds =>
                    {
                        var authors = store.GetAuthorsByIds(authorIds);
                        return await Task.FromResult(authors.ToDictionary(a => a.Id));
                    });

                return loader.LoadAsync(context.Source.AuthorId);
            });
    }
}
