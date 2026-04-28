using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithArgument.C;

/// <summary>
/// Apollo Federation descriptor for the <c>Post</c> entity in the
/// <c>c</c> subgraph. Only exposes the key field <c>id</c>.
/// </summary>
public sealed class PostType : ObjectType<Post>
{
    protected override void Configure(IObjectTypeDescriptor<Post> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
    }

    private static Post? ResolveById(string id)
        => CData.PostsById.TryGetValue(id, out var post) ? post : null;
}
