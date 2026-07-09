namespace HotChocolate.Fusion.Suites.RequiresWithArgument.C;

/// <summary>
/// Seed data for the <c>c</c> subgraph, transcribed from the
/// <c>requires-with-argument</c> audit suite data source.
/// </summary>
internal static class CData
{
    public static readonly IReadOnlyList<Post> Posts =
    [
        new Post { Id = "p1" },
        new Post { Id = "p2" }
    ];

    public static readonly IReadOnlyDictionary<string, Post> PostsById =
        Posts.ToDictionary(static p => p.Id, StringComparer.Ordinal);

    public static readonly IReadOnlyList<Comment> Comments =
    [
        new Comment { Id = "c1", AuthorId = "a2", Body = "c1-body" },
        new Comment { Id = "c2", AuthorId = "a2", Body = "c2-body" },
        new Comment { Id = "c3", AuthorId = "a2", Body = "c3-body" },
        new Comment { Id = "c4", AuthorId = "a1", Body = "c4-body" },
        new Comment { Id = "c5", AuthorId = "a1", Body = "c5-body" },
        new Comment { Id = "c6", AuthorId = "a1", Body = "c6-body" }
    ];

    public static readonly IReadOnlyDictionary<string, Comment> CommentsById =
        Comments.ToDictionary(static c => c.Id, StringComparer.Ordinal);

    /// <summary>
    /// Comments indexed by their parent post identifier.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<Comment>> CommentsByPostId =
        Comments
            .GroupBy(static c =>
            {
                // Derive postId from comment data (c1-c3 -> p1, c4-c6 -> p2).
                return c.Id switch
                {
                    "c1" or "c2" or "c3" => "p1",
                    _ => "p2"
                };
            })
            .ToDictionary(
                static g => g.Key,
                static g => (IReadOnlyList<Comment>)g.ToList(),
                StringComparer.Ordinal);
}
