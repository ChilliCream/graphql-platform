using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresInterface.A;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity in subgraph <c>a</c>.
/// The <c>address</c> field is external. <c>city</c> requires
/// <c>address { id }</c> and <c>country</c> requires
/// <c>address { ... on WorkAddress { id } }</c>.
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
        descriptor.Field(u => u.Address).External().Type<AddressInterfaceType>();

        descriptor
            .Field("city")
            .Type<StringType>()
            .Requires("address { id }")
            .Resolve(ctx =>
            {
                var user = ctx.Parent<User>();
                if (user.Address is not { Id: { Length: > 0 } addressId })
                {
                    return null;
                }

                return AData.AddressesById.TryGetValue(addressId, out var addr)
                    ? addr.City
                    : null;
            });

        descriptor
            .Field("country")
            .Type<StringType>()
            .Requires("address { ... on WorkAddress { id } }")
            .Resolve(ctx =>
            {
                var user = ctx.Parent<User>();
                if (user.Address is not WorkAddress { Id: { Length: > 0 } addressId })
                {
                    return null;
                }

                return AData.AddressesById.TryGetValue(addressId, out var addr) && addr is WorkAddress work
                    ? work.Country
                    : null;
            });
    }

    private static User? ResolveById(string id)
    {
        if (!AData.UsersById.TryGetValue(id, out var record))
        {
            return null;
        }

        return new User { Id = record.Id, Name = record.Name };
    }
}
