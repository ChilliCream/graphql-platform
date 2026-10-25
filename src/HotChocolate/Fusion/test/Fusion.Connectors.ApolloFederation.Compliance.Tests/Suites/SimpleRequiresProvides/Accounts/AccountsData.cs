namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Accounts;

/// <summary>
/// Seed data for the <c>accounts</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/simple-requires-provides/data.ts</c>.
/// </summary>
internal static class AccountsData
{
    /// <summary>
    /// The seeded <see cref="User"/> entities, ordered by id.
    /// </summary>
    public static readonly IReadOnlyList<User> Users =
    [
        new User { Id = "u1", Name = "u-name-1", Username = "u-username-1" },
        new User { Id = "u2", Name = "u-name-2", Username = "u-username-2" }
    ];

    /// <summary>
    /// The seeded <see cref="User"/> entities indexed by their <c>id</c>
    /// field.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, User> ById =
        Users.ToDictionary(static u => u.Id, StringComparer.Ordinal);
}
