using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleEntityCall.Email;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity owned by the <c>email</c>
/// subgraph. Mirrors the fluent descriptor pattern used by
/// <c>HotChocolate.ApolloFederation.CertificationSchema.CodeFirst.Types.UserType</c>.
/// </summary>
public sealed class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(u => u.Id).Type<NonNullType<IdType>>();
        descriptor.Field(u => u.Email).Type<NonNullType<StringType>>();
    }

    private static User ResolveById(string id) => EmailData.ById[id];
}
