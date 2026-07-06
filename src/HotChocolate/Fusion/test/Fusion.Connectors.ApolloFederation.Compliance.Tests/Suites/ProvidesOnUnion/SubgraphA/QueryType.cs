using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnUnion.SubgraphA;

/// <summary>
/// Root <c>Query</c> for <c>subgraph-a</c>. Exposes
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
            .Resolve(_ => SubgraphAData.Media);
    }
}
