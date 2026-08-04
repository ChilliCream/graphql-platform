using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Regular</c> entity in the
/// <c>a</c> subgraph
/// (<c>type Regular implements Account @key(fields: "id") { id, isMain }</c>).
/// </summary>
public sealed class RegularType : ObjectType<Regular>
{
    protected override void Configure(IObjectTypeDescriptor<Regular> descriptor)
    {
        descriptor.Implements<AccountType>();
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(r => r.Id).Type<NonNullType<IdType>>();
        descriptor.Field(r => r.IsMain).Type<NonNullType<BooleanType>>();
    }

    private static Regular? ResolveById(string id)
    {
        var row = AData.FindAccount(id);
        return row is { Typename: "Regular" }
            ? new Regular { Id = row.Id, IsMain = row.IsMain }
            : null;
    }
}
