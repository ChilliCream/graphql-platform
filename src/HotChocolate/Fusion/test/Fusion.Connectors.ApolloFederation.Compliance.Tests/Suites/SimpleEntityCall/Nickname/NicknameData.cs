namespace HotChocolate.Fusion.Suites.SimpleEntityCall.Nickname;

/// <summary>
/// Seed data for the <c>nickname</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/simple-entity-call/data.ts</c>.
/// </summary>
internal static class NicknameData
{
    /// <summary>
    /// The seeded <see cref="User"/> entities, ordered by email.
    /// </summary>
    public static readonly IReadOnlyList<User> Users =
    [
        new User("user1@gmail.com", "user1"),
        new User("user2@gmail.com", "user2")
    ];

    /// <summary>
    /// The seeded <see cref="User"/> entities indexed by their <c>email</c> field.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, User> ByEmail =
        Users.ToDictionary(static u => u.Email, StringComparer.Ordinal);
}
