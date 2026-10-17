using HotChocolate;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace eShop.Accounts;

[Key("id")]
public sealed class User
{
    [GraphQLType(typeof(IdType))]
    public required string Id { get; init; }

    public string? Name { get; init; }

    public string? Username { get; init; }

    public int? Birthday { get; init; }

    [ReferenceResolver]
    public static User? ResolveReference(string id)
        => Query.Users.FirstOrDefault(u => u.Id == id);
}
