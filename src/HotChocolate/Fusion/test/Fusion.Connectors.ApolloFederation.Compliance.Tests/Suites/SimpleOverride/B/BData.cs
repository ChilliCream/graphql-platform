namespace HotChocolate.Fusion.Suites.SimpleOverride.B;

/// <summary>
/// Seed data for the <c>b</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/simple-override/data.ts</c>.
/// </summary>
internal static class BData
{
    /// <summary>
    /// The seeded posts. <c>b</c> owns the canonical <c>createdAt</c> values
    /// (it overrides <c>a.createdAt</c> via <c>@override(from: "a")</c>).
    /// </summary>
    public static readonly IReadOnlyList<Post> Posts =
    [
        new Post { Id = "p1", CreatedAt = "p1-createdAt" },
        new Post { Id = "p2", CreatedAt = "p2-createdAt" }
    ];

    /// <summary>
    /// Lookup of seeded posts keyed by <c>id</c>.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Post> ById =
        Posts.ToDictionary(static p => p.Id!, StringComparer.Ordinal);
}
