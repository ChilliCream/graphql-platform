using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionIntersection.B;

public sealed class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(b => b.Id).Type<NonNullType<IdType>>();
        descriptor.Field(b => b.Title).Shareable().Type<NonNullType<StringType>>();
        descriptor.Field(b => b.BTitle).Type<NonNullType<StringType>>();
    }

    private static Book? ResolveById(string id)
        => string.Equals(id, BData.Media.Id, StringComparison.Ordinal)
            ? BData.Media
            : null;
}
