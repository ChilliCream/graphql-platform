namespace HotChocolate.Fusion.Suites.RequiresCircular.A;

/// <summary>
/// Seed data for posts in subgraph <c>a</c>.
/// </summary>
internal static class PostData
{
    public static readonly IReadOnlyList<Post> Posts =
    [
        new Post { Id = "p1" },
        new Post { Id = "p2" }
    ];

    public static readonly IReadOnlyDictionary<string, Post> ById =
        Posts.ToDictionary(static p => p.Id, StringComparer.Ordinal);
}
