using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NullKeys.C;

/// <summary>
/// Apollo Federation descriptor for the <c>Book</c> entity in the
/// <c>c</c> subgraph (<c>@key(fields: "id")</c>). Owns the <c>author</c>
/// link.
/// </summary>
public sealed class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(b => b.Id).Type<NonNullType<IdType>>();
        descriptor.Field(b => b.Author).Type<AuthorType>();
    }

    private static Book? ResolveById(string id)
        => CData.ById.TryGetValue(id, out var b) ? b : null;
}
