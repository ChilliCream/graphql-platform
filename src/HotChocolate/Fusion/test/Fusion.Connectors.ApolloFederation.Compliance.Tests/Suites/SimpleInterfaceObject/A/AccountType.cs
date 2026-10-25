using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Account</c> interface in the
/// <c>a</c> subgraph
/// (<c>interface Account @key(fields: "id") { id: ID! }</c>). The reference
/// resolver dispatches to the concrete <c>Admin</c> or <c>Regular</c>
/// implementer so the gateway can resolve the concrete <c>__typename</c> for
/// the <c>Account @interfaceObject</c> declared in subgraphs <c>b</c> and
/// <c>c</c>.
/// </summary>
public sealed class AccountType : InterfaceType<IAccount>
{
    protected override void Configure(IInterfaceTypeDescriptor<IAccount> descriptor)
    {
        descriptor.Name("Account");
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
    }

    private static IAccount? ResolveById(string id) => AData.ResolveAccount(id);
}
