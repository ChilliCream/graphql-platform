using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleInaccessible.Age;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity owned by the
/// <c>age</c> subgraph: <c>type User @key(fields: "id") { id: ID, age: Int }</c>.
/// </summary>
public sealed class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(u => u.Id).Type<IdType>();
        descriptor.Field(u => u.Age).Type<IntType>();
    }

    private static User? ResolveById(string id)
        => AgeData.ById.TryGetValue(id, out var user) ? user : null;
}
