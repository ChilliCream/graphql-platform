using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphB;

/// <summary>
/// Root <c>Query</c> for <c>subgraph-b</c>. Exposes
/// <c>media: Media @shareable</c>.
/// </summary>
/// <remarks>
/// The original audit SDL has <c>@provides(fields: "animals { id name }")</c>
/// on <c>Query.media</c>, but HC composition does not support <c>@provides</c>
/// through list-typed fields (SelectionSetValidator.NullableType does not
/// unwrap list wrappers). The <c>@provides</c> is omitted; animal fields
/// are served directly as shareable instead.
/// </remarks>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("media")
            .Type<MediaInterfaceType>()
            .Shareable()
            .Resolve(_ => SubgraphBData.Media);
    }
}
