namespace HotChocolate.Fusion.Suites.RequiresCircular.B;

/// <summary>
/// Seed data for posts in subgraph <c>b</c>.
/// </summary>
internal static class PostData
{
    private static readonly IReadOnlyDictionary<string, string> PostAuthorMap =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["p1"] = "a1",
            ["p2"] = "a2"
        };

    public static readonly IReadOnlySet<string> ById =
        new HashSet<string>(StringComparer.Ordinal) { "p1", "p2" };

    public static Author? GetAuthorForPost(string postId)
        => PostAuthorMap.TryGetValue(postId, out var authorId)
            ? new Author { Id = authorId }
            : null;
}
