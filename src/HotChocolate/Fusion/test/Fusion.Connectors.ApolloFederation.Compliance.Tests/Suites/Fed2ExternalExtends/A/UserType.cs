using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Fed2ExternalExtends.A;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity as extended by
/// the <c>a</c> subgraph. Mirrors the audit SDL
/// <c>type User @key(fields: "id") @extends</c> with fields
/// <c>id: ID! @external</c>, <c>name: String! @external</c>, and <c>rid: ID</c>.
/// The reference resolver returns the seeded <see cref="User"/> by
/// <c>id</c> so subsequent paths can fetch <c>rid</c>.
/// </summary>
public sealed class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .ExtendServiceType()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(u => u.Id).External().Type<NonNullType<IdType>>();
        descriptor.Field(u => u.Name).External().Type<NonNullType<StringType>>();
        descriptor.Field(u => u.Rid).Type<IdType>();
    }

    private static User? ResolveById(string id)
        => AData.ById.TryGetValue(id, out var user)
            ? new User { Id = user.Id, Rid = user.Rid }
            : null;
}
