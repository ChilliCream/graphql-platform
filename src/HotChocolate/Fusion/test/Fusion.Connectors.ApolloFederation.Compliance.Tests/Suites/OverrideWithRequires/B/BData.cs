namespace HotChocolate.Fusion.Suites.OverrideWithRequires.B;

/// <summary>
/// Seed data for subgraph <c>b</c>, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/override-with-requires/data.ts</c>.
/// </summary>
internal static class BData
{
    /// <summary>
    /// The seeded users.
    /// </summary>
    public static readonly IReadOnlyList<User> Users =
    [
        new User { Id = "u1", Name = "u1-name" },
        new User { Id = "u2", Name = "u2-name" },
        new User { Id = "u3", Name = "u3-name" }
    ];

    /// <summary>
    /// Lookup of seeded users keyed by <c>id</c>.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, User> ById =
        Users.ToDictionary(static u => u.Id!, StringComparer.Ordinal);
}
