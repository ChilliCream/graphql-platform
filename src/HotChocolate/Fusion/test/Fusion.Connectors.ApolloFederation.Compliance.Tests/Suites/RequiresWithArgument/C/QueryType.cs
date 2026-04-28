using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithArgument.C;

/// <summary>
/// Root <c>Query</c> for the <c>c</c> subgraph. Exposes the
/// <c>feed: [Post]</c> list field.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("feed")
            .Type<ListType<PostType>>()
            .Resolve(_ => CData.Posts);
    }
}
