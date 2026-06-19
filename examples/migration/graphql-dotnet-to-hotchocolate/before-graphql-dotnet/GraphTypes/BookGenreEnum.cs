using BeforeGraphQLDotNet.Models;
using GraphQL.Types;

namespace BeforeGraphQLDotNet.GraphTypes;

public sealed class BookGenreEnum : EnumerationGraphType<BookGenre>
{
    public BookGenreEnum()
    {
        Name = "BookGenre";
    }
}
