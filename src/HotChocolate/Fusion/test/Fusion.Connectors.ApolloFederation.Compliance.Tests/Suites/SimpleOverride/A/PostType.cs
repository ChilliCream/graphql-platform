using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleOverride.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Post</c> entity in subgraph <c>a</c>.
/// <c>a.createdAt</c> is shareable and resolved by a hardcoded <c>"NEVER"</c>
/// to verify that the gateway routes the field to <c>b</c> via
/// <c>@override(from: "a")</c>.
/// </summary>
public sealed class PostType : ObjectType<Post>
{
    protected override void Configure(IObjectTypeDescriptor<Post> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();

        descriptor
            .Field("createdAt")
            .Type<NonNullType<StringType>>()
            .Shareable()
            .Resolve(_ => "NEVER");
    }

    private static Post? ResolveById(string id)
        => AData.ById.TryGetValue(id, out var post) ? post : null;
}
