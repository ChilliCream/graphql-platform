using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphA;

/// <summary>
/// Apollo Federation descriptor for the <c>Book</c> entity in
/// <c>subgraph-a</c>. Implements <c>Media</c>, keyed by <c>id</c>,
/// with a shareable <c>animals</c> field.
/// </summary>
public sealed class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor
            .Implements<MediaInterfaceType>()
            .Key("id");

        descriptor.Field(b => b.Id).Type<NonNullType<IdType>>();

        descriptor
            .Field(b => b.Animals)
            .Shareable()
            .Type<ListType<AnimalInterfaceType>>();
    }
}
