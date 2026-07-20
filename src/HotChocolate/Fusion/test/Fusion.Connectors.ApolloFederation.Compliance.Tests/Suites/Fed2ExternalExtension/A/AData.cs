namespace HotChocolate.Fusion.Suites.Fed2ExternalExtension.A;

/// <summary>
/// Seed data for the <c>a</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/fed2-external-extension/data.ts</c>.
/// The <c>a</c> subgraph only ever projects <c>id</c> and <c>rid</c>
/// (plus, on the <c>providedRandomUser</c> path, <c>name</c>); other
/// fields are owned by subgraph <c>b</c>.
/// </summary>
internal static class AData
{
    /// <summary>
    /// The seeded <see cref="User"/> entities, ordered by id.
    /// </summary>
    public static readonly IReadOnlyList<User> Users =
    [
        new User { Id = "u1", Rid = "u1-rid", Name = "u1-name" },
        new User { Id = "u2", Rid = "u2-rid", Name = "u2-name" }
    ];

    /// <summary>
    /// The seeded <see cref="User"/> entities indexed by their <c>id</c>
    /// field.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, User> ById =
        Users.ToDictionary(static u => u.Id, StringComparer.Ordinal);
}
