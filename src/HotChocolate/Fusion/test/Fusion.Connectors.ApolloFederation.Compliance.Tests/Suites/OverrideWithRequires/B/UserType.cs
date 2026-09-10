using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.OverrideWithRequires.B;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity in subgraph <c>b</c>.
/// <c>name</c> is the override owner (overrides <c>c.name</c>).
/// </summary>
public sealed class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(u => u.Id).Type<NonNullType<IdType>>();
        descriptor
            .Field(u => u.Name)
            .Type<NonNullType<StringType>>()
            .Override(from: "c");
    }

    private static User? ResolveById(string id)
        => BData.ById.TryGetValue(id, out var user) ? user : null;
}
