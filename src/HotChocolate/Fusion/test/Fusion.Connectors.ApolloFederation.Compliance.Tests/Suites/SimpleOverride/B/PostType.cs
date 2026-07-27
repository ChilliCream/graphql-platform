using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleOverride.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Post</c> entity in subgraph <c>b</c>.
/// <c>createdAt</c> is shareable and overrides the <c>a</c> subgraph's value
/// via <c>@override(from: "a")</c>.
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
            .Field(p => p.CreatedAt)
            .Type<NonNullType<StringType>>()
            .Shareable()
            .Override(from: "a");
    }

    private static Post? ResolveById(string id)
        => BData.ById.TryGetValue(id, out var post) ? post : null;
}
