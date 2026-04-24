using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InputObjectIntersection.A;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity in subgraph
/// <c>a</c> (<c>@key(fields: "id")</c>).
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
    }

    private static User? ResolveById(string id)
        => AData.ById.TryGetValue(id, out var u) ? u : null;
}
