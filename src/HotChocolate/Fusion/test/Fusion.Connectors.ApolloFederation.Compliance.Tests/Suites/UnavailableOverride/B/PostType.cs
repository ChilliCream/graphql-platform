using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnavailableOverride.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Post</c> entity in subgraph <c>b</c>.
/// <c>createdAt</c> declares <c>@override(from: "non-existing")</c>; because
/// the named source is not part of the supergraph, the declaration has no
/// effect and the field remains shareable across subgraphs.
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
            .Override(from: "non-existing");
    }

    private static Post? ResolveById(string id)
        => BData.ById.TryGetValue(id, out var post) ? post : null;
}
