using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphB;

/// <summary>
/// Descriptor for <c>Book</c> in <c>subgraph-b</c>. Not keyed;
/// <c>id</c> is shareable. The <c>animals</c> field is provided inline
/// through <c>@provides</c> on <c>Query.media</c>.
/// </summary>
public sealed class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor.Implements<MediaInterfaceType>();

        descriptor.Field(b => b.Id).Shareable().Type<NonNullType<IdType>>();
        descriptor.Field(b => b.Animals).Shareable().Type<ListType<AnimalInterfaceType>>();
    }
}
