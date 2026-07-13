using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.OverrideWithRequires.C;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity in subgraph <c>c</c>.
/// <c>name</c> is external (overridden by subgraph <c>b</c>); <c>cName</c>
/// requires <c>name</c> and computes <c>c__&lt;name&gt;</c>.
/// </summary>
public sealed class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!, default));

        descriptor.Field(u => u.Id).Type<NonNullType<IdType>>();
        descriptor
            .Field(u => u.Name)
            .External()
            .Type<NonNullType<StringType>>()
            .Resolve(_ => "NEVER");

        descriptor
            .Field("cName")
            .Type<NonNullType<StringType>>()
            .Requires("name")
            .Resolve(ctx =>
            {
                var user = ctx.Parent<User>();
                if (user.Name is null)
                {
                    throw new InvalidOperationException(
                        "cName requires the external 'name' field on the parent entity.");
                }
                return $"c__{user.Name}";
            });
    }

    private static User? ResolveById(string id, [Map("name")] string? name)
    {
        if (!CData.ById.TryGetValue(id, out var user))
        {
            return null;
        }

        return new User { Id = user.Id, Name = name is null ? null : user.Name };
    }
}
