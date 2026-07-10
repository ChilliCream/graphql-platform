using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NullKeys.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Book</c> entity in the
/// <c>a</c> subgraph (<c>@key(fields: "upc")</c>).
/// </summary>
public sealed class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor
            .Key("upc")
            .ResolveReferenceWith(_ => ResolveByUpc(default!));

        descriptor.Field(b => b.Upc).Type<NonNullType<IdType>>();
    }

    private static Book? ResolveByUpc(string upc)
        => AData.ByUpc.TryGetValue(upc, out var b) ? b : null;
}
