namespace HotChocolate.Fusion.Suites.SimpleInaccessible.Age;

/// <summary>
/// Seed data for the <c>age</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/simple-inaccessible/data.ts</c>.
/// </summary>
internal static class AgeData
{
    /// <summary>
    /// The seeded users projected with <c>id</c> and <c>age</c>.
    /// </summary>
    public static readonly IReadOnlyList<User> Users =
    [
        new User { Id = "u1", Age = 11 },
        new User { Id = "u2", Age = 22 }
    ];

    /// <summary>
    /// Lookup of seeded users keyed by <c>id</c>.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, User> ById =
        Users.ToDictionary(static u => u.Id!, StringComparer.Ordinal);
}
