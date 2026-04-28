using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresInterface.B;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity in subgraph <c>b</c>.
/// Owns the <c>address: Address</c> field (shareable) and
/// <c>name: String!</c> (shareable).
/// </summary>
public sealed class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(u => u.Id).Type<NonNullType<IdType>>();
        descriptor.Field(u => u.Name).Shareable().Type<NonNullType<StringType>>();

        descriptor
            .Field("address")
            .Type<AddressInterfaceType>()
            .Shareable()
            .Resolve(ctx =>
            {
                var user = ctx.Parent<User>();
                if (user.AddressId is not { Length: > 0 } addressId)
                {
                    return null;
                }

                if (!BData.AddressesById.TryGetValue(addressId, out var addr))
                {
                    return null;
                }

                return (object?)addr;
            });

        // Hide the AddressId property from the schema.
        descriptor.Field(u => u.AddressId).Ignore();
    }

    private static User? ResolveById(string id)
        => BData.UsersById.TryGetValue(id, out var u) ? u : null;
}
