using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleEntityCall.Nickname;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity as extended by the
/// <c>nickname</c> subgraph. Mirrors
/// <c>HotChocolate.ApolloFederation.CertificationSchema.CodeFirst.Types.UserType</c>.
/// </summary>
public sealed class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .ExtendServiceType()
            .Key("email")
            .ResolveReferenceWith(_ => ResolveByEmail(default!));

        descriptor.Field(u => u.Email).External().Type<NonNullType<StringType>>();
        descriptor.Field(u => u.Nickname).Type<NonNullType<StringType>>();
    }

    private static User ResolveByEmail(string email) => NicknameData.ByEmail[email];
}
