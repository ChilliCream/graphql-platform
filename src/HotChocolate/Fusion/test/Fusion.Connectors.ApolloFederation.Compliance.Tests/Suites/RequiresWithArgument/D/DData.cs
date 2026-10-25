namespace HotChocolate.Fusion.Suites.RequiresWithArgument.D;

/// <summary>
/// Seed data for the <c>d</c> subgraph, transcribed from the
/// <c>requires-with-argument</c> audit suite data source.
/// </summary>
internal static class DData
{
    public static readonly IReadOnlyList<Author> Authors =
    [
        new Author { Id = "a1", Name = "a1-name" },
        new Author { Id = "a2", Name = "a2-name" }
    ];

    public static readonly IReadOnlyDictionary<string, Author> AuthorsById =
        Authors.ToDictionary(static a => a.Id, StringComparer.Ordinal);

    public static readonly IReadOnlyDictionary<string, string> PostsById =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["p1"] = "p1",
            ["p2"] = "p2"
        };

    internal sealed record CommentSeed(string Id, string PostId, string AuthorId);

    public static readonly IReadOnlyList<CommentSeed> Comments =
    [
        new("c1", "p1", "a2"),
        new("c2", "p1", "a2"),
        new("c3", "p1", "a2"),
        new("c4", "p2", "a1"),
        new("c5", "p2", "a1"),
        new("c6", "p2", "a1")
    ];

    public static readonly IReadOnlyDictionary<string, CommentSeed> CommentsById =
        Comments.ToDictionary(static c => c.Id, StringComparer.Ordinal);

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<CommentSeed>> CommentsByPostId =
        Comments
            .GroupBy(static c => c.PostId)
            .ToDictionary(
                static g => g.Key,
                static g => (IReadOnlyList<CommentSeed>)g.ToList(),
                StringComparer.Ordinal);
}
