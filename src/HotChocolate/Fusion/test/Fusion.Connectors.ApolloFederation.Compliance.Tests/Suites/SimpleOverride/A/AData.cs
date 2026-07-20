namespace HotChocolate.Fusion.Suites.SimpleOverride.A;

/// <summary>
/// Seed data for the <c>a</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/simple-override/data.ts</c>.
/// </summary>
internal static class AData
{
    /// <summary>
    /// The seeded posts. <c>a</c> only knows the <c>id</c>; the
    /// <c>createdAt</c> resolver is hardcoded to return <c>"NEVER"</c>.
    /// </summary>
    public static readonly IReadOnlyList<Post> Posts =
    [
        new Post { Id = "p1" },
        new Post { Id = "p2" }
    ];

    /// <summary>
    /// Lookup of seeded posts keyed by <c>id</c>.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Post> ById =
        Posts.ToDictionary(static p => p.Id!, StringComparer.Ordinal);
}
