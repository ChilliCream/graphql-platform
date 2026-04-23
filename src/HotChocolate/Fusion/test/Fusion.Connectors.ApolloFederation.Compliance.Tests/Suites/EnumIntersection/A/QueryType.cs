using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.EnumIntersection.A;

/// <summary>
/// Root <c>Query</c> for the <c>a</c> subgraph. Exposes <c>users: [User]</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("users")
            .Type<ListType<UserGraphType>>()
            .Resolve(_ => AData.Users);
    }
}
