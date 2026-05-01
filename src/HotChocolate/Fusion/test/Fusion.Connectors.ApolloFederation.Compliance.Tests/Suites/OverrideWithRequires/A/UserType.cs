using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.OverrideWithRequires.A;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity in subgraph <c>a</c>.
/// <c>name</c> is external; <c>aName</c> requires <c>name</c> and computes
/// <c>a__&lt;name&gt;</c>. The reference resolver returns just the key so the
/// federation external setter populates <c>name</c> on the parent before
/// <c>aName</c> runs.
/// </summary>
public sealed class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(u => u.Id).Type<NonNullType<IdType>>();
        descriptor.Field(u => u.Name).External().Type<NonNullType<StringType>>();

        descriptor
            .Field("aName")
            .Type<NonNullType<StringType>>()
            .Requires("name")
            .Resolve(ctx =>
            {
                var user = ctx.Parent<User>();
                if (user.Name is null)
                {
                    throw new InvalidOperationException(
                        "aName requires the external 'name' field on the parent entity.");
                }
                return $"a__{user.Name}";
            });
    }

    private static User ResolveById(string id) => new() { Id = id };
}
