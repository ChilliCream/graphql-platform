using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnUnion.SubgraphB;

/// <summary>
/// Root <c>Query</c> for <c>subgraph-b</c>. Exposes
/// <c>media: [Media] @shareable</c>.
/// </summary>
/// <remarks>
/// The original audit SDL has <c>@provides(fields: "... on Book { title }")</c>
/// on <c>Query.media</c>, but HC composition does not support <c>@provides</c>
/// through list-typed fields or union inline fragments
/// (SelectionSetValidator.NullableType does not unwrap list wrappers and
/// union type conditions are not resolved). The <c>@provides</c> is omitted;
/// Book.title is served directly as shareable instead.
/// </remarks>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("media")
            .Type<ListType<MediaUnionType>>()
            .Shareable()
            .Resolve(_ => SubgraphBData.Media);
    }
}
