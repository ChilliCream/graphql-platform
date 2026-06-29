using HotChocolate.ApolloFederation.Types;
using HotChocolate.Fusion.Suites.AbstractTypes.Products;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Users;

public sealed class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Key("email")
            .ResolveReferenceWith(_ => ResolveByEmail(default!));

        descriptor.Field(u => u.Email).Type<NonNullType<IdType>>();
        descriptor.Field(u => u.Name).Type<StringType>();

        descriptor
            .Field("totalProductsCreated")
            .Shareable()
            .Type<IntType>()
            .Resolve(ctx =>
            {
                var user = ctx.Parent<User>();
                return UserData.CountProductsCreated(user.InternalId);
            });

        descriptor.Field(u => u.InternalId).Ignore();
    }

    private static User? ResolveByEmail(string email)
        => UserData.UsersByEmail.TryGetValue(email, out var user) ? user : null;
}
