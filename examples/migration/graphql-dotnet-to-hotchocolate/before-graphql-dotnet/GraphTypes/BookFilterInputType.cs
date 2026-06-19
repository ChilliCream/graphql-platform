using BeforeGraphQLDotNet.Models;
using GraphQL.Types;

namespace BeforeGraphQLDotNet.GraphTypes;

public sealed class BookFilterInputType : InputObjectGraphType<BookFilter>
{
    public BookFilterInputType()
    {
        Name = "BookFilterInput";

        Field<BookGenreEnum>("genre");
        Field<StringGraphType>("titleContains");
    }
}
