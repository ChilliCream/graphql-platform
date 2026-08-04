namespace HotChocolate.Fusion.Suites.SimpleEntityCall.Email;

/// <summary>
/// Seed data for the <c>email</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/simple-entity-call/data.ts</c>.
/// </summary>
internal static class EmailData
{
    /// <summary>
    /// The seeded <see cref="User"/> entities, ordered by id.
    /// </summary>
    public static readonly IReadOnlyList<User> Users =
    [
        new User("1", "user1@gmail.com"),
        new User("2", "user2@gmail.com")
    ];

    /// <summary>
    /// The seeded <see cref="User"/> entities indexed by their <c>id</c> field.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, User> ById =
        Users.ToDictionary(static u => u.Id, StringComparer.Ordinal);
}
