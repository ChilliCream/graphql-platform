using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.C;

/// <summary>
/// Apollo Federation descriptor for the <c>Account</c> interface object in the
/// <c>c</c> subgraph
/// (<c>type Account @key(fields: "id") @interfaceObject { id, isActive @shareable }</c>).
/// The audit resolver always reports <c>isActive: false</c>.
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
        descriptor.Field(a => a.IsActive).Shareable().Type<NonNullType<BooleanType>>().Resolve(_ => false);
    }

    private static Account ResolveById(string id) => new() { Id = id };
}
