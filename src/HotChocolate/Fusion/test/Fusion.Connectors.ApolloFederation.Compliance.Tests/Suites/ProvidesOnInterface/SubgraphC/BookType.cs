using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphC;

/// <summary>
/// Apollo Federation descriptor for the <c>Book</c> entity in
/// <c>subgraph-c</c>. Keyed by <c>id</c>, with shareable <c>animals</c>.
/// </summary>
public sealed class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor
            .Implements<MediaInterfaceType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(b => b.Id).Type<NonNullType<IdType>>();

        descriptor
            .Field(b => b.Animals)
            .Shareable()
            .Type<ListType<AnimalInterfaceType>>();
    }

    private static Book? ResolveById(string id)
        => SubgraphCData.BooksById.TryGetValue(id, out var book) ? book : null;
}
