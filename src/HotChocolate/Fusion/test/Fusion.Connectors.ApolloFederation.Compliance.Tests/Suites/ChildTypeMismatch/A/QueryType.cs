using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ChildTypeMismatch.A;

/// <summary>
/// Root <c>Query</c> type for the <c>a</c> subgraph. Exposes
/// <c>users: [User!]!</c> returning all seeded users (id only).
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("users")
            .Type<NonNullType<ListType<NonNullType<UserType>>>>()
            .Resolve(_ => AData.Users);
    }
}
