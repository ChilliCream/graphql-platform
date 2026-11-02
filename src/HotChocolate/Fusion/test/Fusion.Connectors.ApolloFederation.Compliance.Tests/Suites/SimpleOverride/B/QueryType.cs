using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleOverride.B;

/// <summary>
/// Root <c>Query</c> for subgraph <c>b</c>. Exposes a shareable
/// <c>feed</c> plus a private <c>bFeed</c> (returning the first seeded
/// post only).
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("feed")
            .Type<ListType<PostType>>()
            .Shareable()
            .Resolve(_ => BData.Posts);

        descriptor
            .Field("bFeed")
            .Type<ListType<PostType>>()
            .Resolve(_ => new[] { BData.Posts[0] });
    }
}
