using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.OverrideTypeInterface.A;

/// <summary>
/// Apollo Federation descriptor for the <c>ImagePost</c> entity in subgraph
/// <c>a</c>. <c>createdAt</c> is hardcoded to <c>"NEVER"</c> so the audit can
/// confirm the gateway routes <c>createdAt</c> through subgraph <c>b</c>'s
/// <c>@override(from: "a")</c> declaration.
/// </summary>
public sealed class ImagePostType : ObjectType<ImagePost>
{
    protected override void Configure(IObjectTypeDescriptor<ImagePost> descriptor)
    {
        descriptor
            .Implements<PostInterfaceType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();

        descriptor
            .Field("createdAt")
            .Type<NonNullType<StringType>>()
            .Resolve(_ => "NEVER");

        descriptor.Ignore(p => p.CreatedAt);
    }

    private static ImagePost? ResolveById(string id)
        => AData.ById.TryGetValue(id, out var post) ? post : null;
}
