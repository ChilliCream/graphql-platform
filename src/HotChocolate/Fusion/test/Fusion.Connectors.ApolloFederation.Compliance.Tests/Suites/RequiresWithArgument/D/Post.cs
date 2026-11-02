namespace HotChocolate.Fusion.Suites.RequiresWithArgument.D;

/// <summary>
/// The <c>Post</c> entity in the <c>d</c> subgraph
/// (<c>@key(fields: "id")</c>). Extends <c>Post</c> with <c>author</c>
/// (via <c>@requires</c>) and <c>comments(limit)</c>. The
/// <c>AuthorId</c> property is populated from the required comments
/// data when present.
/// </summary>
public sealed class Post
{
    public string Id { get; set; } = default!;

    public string? AuthorId { get; set; }

    public IReadOnlyList<Comment>? Comments { get; set; }
}
