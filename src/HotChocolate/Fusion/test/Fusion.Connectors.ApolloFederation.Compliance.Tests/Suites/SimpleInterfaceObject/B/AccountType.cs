using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Account</c> interface object in the
/// <c>b</c> subgraph
/// (<c>type Account @key(fields: "id") @interfaceObject { id, name: String! }</c>).
/// </summary>
public sealed class AccountType : ObjectType<Account>
{
    protected override void Configure(IObjectTypeDescriptor<Account> descriptor)
    {
        descriptor
            .InterfaceObject()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.Name).Type<NonNullType<StringType>>();
    }

    private static Account? ResolveById(string id)
        => BData.Accounts.FirstOrDefault(
            account => string.Equals(account.Id, id, StringComparison.Ordinal));
}
