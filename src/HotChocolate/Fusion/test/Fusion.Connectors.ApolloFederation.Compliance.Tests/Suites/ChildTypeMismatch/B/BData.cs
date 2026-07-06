namespace HotChocolate.Fusion.Suites.ChildTypeMismatch.B;

/// <summary>
/// Seed data for the <c>b</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/child-type-mismatch/data.ts</c>
/// and the <c>accounts()</c> resolver in <c>b.subgraph.ts</c>.
/// </summary>
internal static class BData
{
    /// <summary>
    /// The seeded <see cref="User"/> entities.
    /// </summary>
    public static readonly IReadOnlyList<User> Users =
    [
        new User { Id = "u1", Name = "u1-name" }
    ];

    /// <summary>
    /// The seeded <see cref="User"/> entities indexed by their <c>id</c> field.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, User> UsersById =
        Users.ToDictionary(static u => u.Id, StringComparer.Ordinal);

    /// <summary>
    /// The full list of accounts (User + Admin), used by
    /// <c>Query.accounts</c> and the <c>similarAccounts</c> resolvers.
    /// </summary>
    public static readonly IReadOnlyList<object> Accounts =
    [
        Users[0],
        new Admin { Id = "a1", Name = "a1-name" }
    ];
}
