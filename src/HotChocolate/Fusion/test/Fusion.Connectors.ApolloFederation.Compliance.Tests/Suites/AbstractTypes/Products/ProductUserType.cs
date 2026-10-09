using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Products;

public sealed class ProductUserType : ObjectType<UserRef>
{
    protected override void Configure(IObjectTypeDescriptor<UserRef> descriptor)
    {
        descriptor.Name("User");

        descriptor
            .Key("email")
            .ResolveReferenceWith(_ => ResolveByEmail(default!));

        descriptor.Field(u => u.Email).Type<NonNullType<IdType>>();

        descriptor
            .Field("totalProductsCreated")
            .Shareable()
            .Type<IntType>()
            .Resolve(ctx =>
            {
                var user = ctx.Parent<UserRef>();
                return ProductData.CountProductsByUser(user.InternalId);
            });

        descriptor.Field(u => u.InternalId).Ignore();
    }

    private static UserRef? ResolveByEmail(string email)
    {
        foreach (var u in ProductData.Users)
        {
            if (u.Email == email)
            {
                return u;
            }
        }

        return null;
    }
}
