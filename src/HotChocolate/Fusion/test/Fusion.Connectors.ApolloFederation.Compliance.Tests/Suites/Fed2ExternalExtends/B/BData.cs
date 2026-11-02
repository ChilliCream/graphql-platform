namespace HotChocolate.Fusion.Suites.Fed2ExternalExtends.B;

/// <summary>
/// Seed data for the <c>b</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/fed2-external-extends/data.ts</c>.
/// </summary>
internal static class BData
{
    /// <summary>
    /// The seeded <see cref="User"/> entities, ordered by id.
    /// </summary>
    public static readonly IReadOnlyList<User> Users =
    [
        new User("u1", "u1-name", "u1-nickname"),
        new User("u2", "u2-name", "u2-nickname")
    ];

    /// <summary>
    /// The seeded <see cref="User"/> entities indexed by their <c>id</c>
    /// field.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, User> ById =
        Users.ToDictionary(static u => u.Id, StringComparer.Ordinal);
}
