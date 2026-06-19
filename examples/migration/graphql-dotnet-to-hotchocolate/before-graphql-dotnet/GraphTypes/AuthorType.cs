using BeforeGraphQLDotNet.Models;
using BeforeGraphQLDotNet.Services;
using GraphQL.Types;

namespace BeforeGraphQLDotNet.GraphTypes;

public sealed class AuthorType : ObjectGraphType<Author>
{
    public AuthorType(BookDataStore store)
    {
        Name = "Author";

        Field<NonNullGraphType<IdGraphType>>("id")
            .Resolve(context => context.Source.Id);

        Field<NonNullGraphType<StringGraphType>>("name")
            .Resolve(context => context.Source.Name);

        Field<NonNullGraphType<ListGraphType<NonNullGraphType<BookType>>>, IEnumerable<Book>>("books")
            .Resolve(context => store.GetBooksByAuthorId(context.Source.Id));
    }
}
