using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnUnion.SubgraphB;

/// <summary>
/// Root <c>Query</c> for <c>subgraph-b</c>. Exposes
/// <c>media: [Media] @shareable</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("media")
            .Type<ListType<MediaUnionType>>()
            .Shareable()
            .Provides("... on Book { title }")
            .Resolve(_ => SubgraphBData.Media);
    }
}
