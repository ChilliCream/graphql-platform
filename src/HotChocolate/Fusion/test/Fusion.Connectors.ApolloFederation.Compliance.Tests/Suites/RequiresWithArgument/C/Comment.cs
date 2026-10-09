namespace HotChocolate.Fusion.Suites.RequiresWithArgument.C;

/// <summary>
/// The <c>Comment</c> entity owned by the <c>c</c> subgraph
/// (<c>@key(fields: "id")</c>). Owns <c>authorId</c> and <c>body</c>.
/// </summary>
public sealed class Comment
{
    public string Id { get; init; } = default!;

    public string? AuthorId { get; init; }

    public string Body { get; init; } = default!;
}
