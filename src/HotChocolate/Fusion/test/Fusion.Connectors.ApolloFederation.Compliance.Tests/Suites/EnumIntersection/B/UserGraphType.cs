using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.EnumIntersection.B;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity in subgraph
/// <c>b</c> (<c>extend type User @key(fields: "id")</c>).
/// </summary>
public sealed class UserGraphType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Name("User");

        descriptor
            .ExtendServiceType()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(u => u.Id).Type<IdType>();
        descriptor.Field(u => u.Type).Shareable().Type<UserTypeType>();
    }

    private static User? ResolveById(string id)
        => BData.ById.TryGetValue(id, out var u) ? u : null;
}
