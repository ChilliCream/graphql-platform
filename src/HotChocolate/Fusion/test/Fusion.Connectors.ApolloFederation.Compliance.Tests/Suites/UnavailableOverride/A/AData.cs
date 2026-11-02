namespace HotChocolate.Fusion.Suites.UnavailableOverride.A;

/// <summary>
/// Seed data for subgraph <c>a</c>, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/unavailable-override/data.ts</c>.
/// </summary>
internal static class AData
{
    /// <summary>
    /// The seeded posts. Both subgraphs hold the canonical
    /// <c>createdAt</c> values; the <c>@override(from: "non-existing")</c>
    /// declared by subgraph <c>b</c> never finds a real owner to override.
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
