using BeforeGraphQLDotNet.GraphTypes;
using BeforeGraphQLDotNet.Models;
using BeforeGraphQLDotNet.Services;
using GraphQL.Types;

namespace BeforeGraphQLDotNet.Schema;

public sealed class Subscription : ObjectGraphType
{
    public Subscription(IBookEventService events)
    {
        Name = "Subscription";

        Field<NonNullGraphType<BookType>, Book>("onBookAdded")
            .ResolveStream(context => events.BookAdded);
    }
}
