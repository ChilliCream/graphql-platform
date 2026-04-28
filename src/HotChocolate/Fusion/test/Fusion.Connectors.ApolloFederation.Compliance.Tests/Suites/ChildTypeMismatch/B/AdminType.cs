using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ChildTypeMismatch.B;

/// <summary>
/// Descriptor for the <c>Admin</c> type in the <c>b</c> subgraph.
/// Not an entity (no <c>@key</c>). <c>name</c> is <c>@shareable</c>.
/// <c>similarAccounts</c> returns the full accounts list.
/// </summary>
public sealed class AdminType : ObjectType<Admin>
{
    protected override void Configure(IObjectTypeDescriptor<Admin> descriptor)
    {
        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.Name).Shareable().Type<StringType>();

        descriptor
            .Field("similarAccounts")
            .Type<NonNullType<ListType<NonNullType<AccountType>>>>()
            .Resolve(_ => BData.Accounts);
    }
}
