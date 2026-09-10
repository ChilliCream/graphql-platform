using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnavailableOverride.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Post</c> entity in subgraph <c>a</c>.
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
            .Shareable();
    }

    private static Post? ResolveById(string id)
        => AData.ById.TryGetValue(id, out var post) ? post : null;
}
