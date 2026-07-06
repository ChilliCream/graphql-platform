namespace HotChocolate.Fusion.Suites.OverrideTypeInterface.B;

/// <summary>
/// Seed data for subgraph <c>b</c>, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/override-type-interface/data.ts</c>.
/// </summary>
internal static class BData
{
    /// <summary>
    /// The seeded <c>ImagePost</c>s. Subgraph <c>b</c> owns the canonical
    /// <c>createdAt</c> values via <c>@override(from: "a")</c>.
    /// </summary>
    public static readonly IReadOnlyList<ImagePost> ImagePosts =
    [
        new ImagePost { Id = "i1", CreatedAt = "i1-createdAt" },
        new ImagePost { Id = "i2", CreatedAt = "i2-createdAt" }
    ];

    /// <summary>
    /// The seeded <c>TextPost</c>s.
    /// </summary>
    public static readonly IReadOnlyList<TextPost> TextPosts =
    [
        new TextPost { Id = "t1", CreatedAt = "t1-createdAt", Body = "t1-body" },
        new TextPost { Id = "t2", CreatedAt = "t2-createdAt", Body = "t2-body" }
    ];

    /// <summary>
    /// Lookup of <c>ImagePost</c>s keyed by <c>id</c>.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, ImagePost> ImagePostsById =
        ImagePosts.ToDictionary(static p => p.Id, StringComparer.Ordinal);

    /// <summary>
    /// Lookup of <c>TextPost</c>s keyed by <c>id</c>.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, TextPost> TextPostsById =
        TextPosts.ToDictionary(static p => p.Id, StringComparer.Ordinal);
}
