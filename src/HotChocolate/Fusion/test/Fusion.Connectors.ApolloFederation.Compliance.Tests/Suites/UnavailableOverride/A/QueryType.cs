using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnavailableOverride.A;

/// <summary>
/// Root <c>Query</c> for subgraph <c>a</c>. Exposes a shareable
/// <c>feed</c> plus a private <c>aFeed</c> (returning the second seeded
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
            .Resolve(_ => AData.Posts);

        descriptor
            .Field("aFeed")
            .Type<ListType<PostType>>()
            .Resolve(_ => new[] { AData.Posts[1] });
    }
}
