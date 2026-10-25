using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.OverrideTypeInterface.B;

/// <summary>
/// Root <c>Query</c> for subgraph <c>b</c>. Exposes
/// <c>anotherFeed: [AnotherPost]</c> returning the seeded <c>ImagePost</c>
/// instances cast to <c>AnotherPost</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("anotherFeed")
            .Type<ListType<AnotherPostInterfaceType>>()
            .Resolve(_ => BData.ImagePosts.Cast<IAnotherPost>().ToArray());
    }
}
