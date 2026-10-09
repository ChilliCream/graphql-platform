namespace HotChocolate.Fusion.Suites.ChildTypeMismatch.A;

/// <summary>
/// Seed data for the <c>a</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/child-type-mismatch/data.ts</c>.
/// </summary>
internal static class AData
{
    /// <summary>
    /// The seeded <see cref="User"/> instances. The <c>a</c> subgraph only
    /// exposes <c>id</c>.
    /// </summary>
    public static readonly IReadOnlyList<User> Users =
    [
        new User { Id = "u1" }
    ];
}
