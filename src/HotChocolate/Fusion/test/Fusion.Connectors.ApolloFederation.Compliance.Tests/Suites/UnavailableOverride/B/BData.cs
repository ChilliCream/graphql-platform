namespace HotChocolate.Fusion.Suites.UnavailableOverride.B;

/// <summary>
/// Seed data for subgraph <c>b</c>, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/unavailable-override/data.ts</c>.
/// </summary>
internal static class BData
{
    /// <summary>
    /// The seeded posts. Same canonical values as subgraph <c>a</c>; the
    /// <c>@override(from: "non-existing")</c> declared by subgraph <c>b</c>
    /// targets a subgraph that does not participate in the supergraph.
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
