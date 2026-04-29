namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Reviews;

/// <summary>
/// Seed data for the <c>reviews</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/simple-requires-provides/data.ts</c>.
/// The reviews subgraph also remembers the inline username for users so the
/// <c>@provides(fields: "username")</c> path on <c>Review.author</c> can ship
/// the value alongside the entity reference.
/// </summary>
internal static class ReviewsData
{
    /// <summary>
    /// The seeded <see cref="Review"/> entities, ordered by id.
    /// </summary>
    public static readonly IReadOnlyList<Review> Reviews =
    [
        new Review { Id = "r1", Body = "r-body-1", AuthorId = "u1", ProductUpc = "p1" },
        new Review { Id = "r2", Body = "r-body-2", AuthorId = "u1", ProductUpc = "p2" }
    ];

    /// <summary>
    /// The seeded <see cref="Review"/> entities indexed by their <c>id</c>
    /// field.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Review> ById =
        Reviews.ToDictionary(static r => r.Id, StringComparer.Ordinal);

    /// <summary>
    /// Inline mirror of the <c>accounts</c> subgraph user table so the
    /// reviews subgraph can populate the external <c>username</c> field
    /// when serving the <c>@provides(fields: "username")</c> path.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> UsernameById =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["u1"] = "u-username-1",
            ["u2"] = "u-username-2"
        };
}
