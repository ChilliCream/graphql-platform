using HotChocolate;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace eShop.Reviews;

[ExtendServiceType]
[Key("id")]
public sealed class User
{
    internal static readonly Dictionary<string, User> Users =
        new(StringComparer.Ordinal)
    {
        ["1"] = new() { Id = "1", Username = "urigo" },
        ["2"] = new() { Id = "2", Username = "dotansimha" },
        ["3"] = new() { Id = "3", Username = "kamilkisiela" },
        ["4"] = new() { Id = "4", Username = "ardatan" },
        ["5"] = new() { Id = "5", Username = "gilgardosh" },
        ["6"] = new() { Id = "6", Username = "laurin" }
    };

    [External]
    [GraphQLType(typeof(IdType))]
    public string Id { get; set; } = default!;

    [External]
    public string? Username { get; set; }

    public IEnumerable<Review> GetReviews()
        => ReviewRepository.GetByUserId(Id);

    [ReferenceResolver]
    public static User ResolveReference(string id)
        => Users.TryGetValue(id, out var user) ? user : new() { Id = id, Username = "user" };
}
