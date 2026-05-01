namespace HotChocolate.Fusion.Suites.OverrideTypeInterface.A;

/// <summary>
/// Seed data for subgraph <c>a</c>, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/override-type-interface/data.ts</c>.
/// </summary>
internal static class AData
{
    /// <summary>
    /// The seeded <c>ImagePost</c>s. Subgraph <c>a</c>'s <c>createdAt</c>
    /// resolver returns a hardcoded <c>"NEVER"</c> so that the audit can
    /// verify the override path through subgraph <c>b</c>.
    /// </summary>
    public static readonly IReadOnlyList<ImagePost> ImagePosts =
    [
        new ImagePost { Id = "i1", CreatedAt = "i1-createdAt" },
        new ImagePost { Id = "i2", CreatedAt = "i2-createdAt" }
    ];

    /// <summary>
    /// Lookup of seeded <c>ImagePost</c>s keyed by <c>id</c>.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, ImagePost> ById =
        ImagePosts.ToDictionary(static p => p.Id, StringComparer.Ordinal);
}
