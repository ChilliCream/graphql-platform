using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresCircular.A;

/// <summary>
/// Root <c>Query</c> for subgraph <c>a</c> with the <c>feed</c> field
/// that returns all posts (only keys).
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("feed")
            .Type<ListType<PostType>>()
            .Resolve(_ => PostData.Posts.Select(p => new Post { Id = p.Id }).ToArray());
    }
}
