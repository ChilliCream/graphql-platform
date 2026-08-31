using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphB;

/// <summary>
/// Root <c>Query</c> for <c>subgraph-b</c>. Exposes
/// <c>media: Media @shareable</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("media")
            .Type<MediaInterfaceType>()
            .Shareable()
            .Provides("animals { id name }")
            .Resolve(_ => SubgraphBData.Media);
    }
}
