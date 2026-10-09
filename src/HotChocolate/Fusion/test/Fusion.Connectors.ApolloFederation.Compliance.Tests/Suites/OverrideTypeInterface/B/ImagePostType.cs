using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.OverrideTypeInterface.B;

/// <summary>
/// Apollo Federation descriptor for the <c>ImagePost</c> entity in subgraph
/// <c>b</c>. Implements <c>AnotherPost</c> only (not <c>Post</c>);
/// <c>createdAt</c> is the override owner via <c>@override(from: "a")</c>.
/// </summary>
public sealed class ImagePostType : ObjectType<ImagePost>
{
    protected override void Configure(IObjectTypeDescriptor<ImagePost> descriptor)
    {
        descriptor
            .Implements<AnotherPostInterfaceType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor
            .Field(p => p.CreatedAt)
            .Type<NonNullType<StringType>>()
            .Override(from: "a");
    }

    private static ImagePost? ResolveById(string id)
        => BData.ImagePostsById.TryGetValue(id, out var post) ? post : null;
}
