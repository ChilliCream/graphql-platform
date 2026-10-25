using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.OverrideTypeInterface.A;

/// <summary>
/// Root <c>Query</c> for subgraph <c>a</c>. Exposes <c>feed: [Post]</c>
/// returning the seeded <c>ImagePost</c> instances cast to the
/// <c>Post</c> interface.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("feed")
            .Type<ListType<PostInterfaceType>>()
            .Resolve(_ => AData.ImagePosts.Cast<IPost>().ToArray());
    }
}
