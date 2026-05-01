using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.OverrideTypeInterface.B;

/// <summary>
/// Apollo Federation descriptor for the <c>TextPost</c> entity in subgraph
/// <c>b</c>. Implements <c>Post</c>; <c>createdAt</c> is owned locally.
/// </summary>
public sealed class TextPostType : ObjectType<TextPost>
{
    protected override void Configure(IObjectTypeDescriptor<TextPost> descriptor)
    {
        descriptor
            .Implements<PostInterfaceType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.CreatedAt).Type<NonNullType<StringType>>();
        descriptor.Field(p => p.Body).Type<NonNullType<StringType>>();
    }

    private static TextPost? ResolveById(string id)
        => BData.TextPostsById.TryGetValue(id, out var post) ? post : null;
}
