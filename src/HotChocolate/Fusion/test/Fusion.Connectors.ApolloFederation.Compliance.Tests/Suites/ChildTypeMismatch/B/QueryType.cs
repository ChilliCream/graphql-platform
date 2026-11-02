using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ChildTypeMismatch.B;

/// <summary>
/// Root <c>Query</c> type for the <c>b</c> subgraph. Exposes
/// <c>accounts: [Account!]!</c> returning all users and admins.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("accounts")
            .Type<NonNullType<ListType<NonNullType<AccountType>>>>()
            .Resolve(_ => BData.Accounts);
    }
}
