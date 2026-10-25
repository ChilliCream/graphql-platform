using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Accounts;

/// <summary>
/// Root <c>Query</c> for the <c>accounts</c> subgraph. Exposes the
/// <c>me: User</c> field that returns the first seeded user.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("me")
            .Type<UserType>()
            .Resolve(_ => AccountsData.Users[0]);
    }
}
