using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.EnumIntersection.A;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity in subgraph
/// <c>a</c> (<c>@key(fields: "id")</c>). Owns <c>type</c> as shareable.
/// </summary>
public sealed class UserGraphType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Name("User");

        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(u => u.Id).Type<IdType>();
        descriptor.Field(u => u.Type).Shareable().Type<UserTypeType>();
    }

    private static User? ResolveById(string id)
        => AData.ById.TryGetValue(id, out var u) ? u : null;
}
