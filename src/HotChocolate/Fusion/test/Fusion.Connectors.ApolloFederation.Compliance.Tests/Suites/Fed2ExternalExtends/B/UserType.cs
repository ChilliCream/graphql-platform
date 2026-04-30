using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Fed2ExternalExtends.B;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity owned by the
/// <c>b</c> subgraph. Mirrors the audit SDL
/// <c>type User @key(fields: "id")</c> with fields
/// <c>id: ID!</c>, <c>name: String! @shareable</c>, and <c>nickname: String</c>.
/// The reference resolver looks up users by <c>id</c>.
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
        descriptor.Field(u => u.Nickname).Type<StringType>();
    }

    private static User? ResolveById(string id)
        => BData.ById.TryGetValue(id, out var user) ? user : null;
}
