namespace HotChocolate.Fusion.Suites.RequiresWithArgument.D;

/// <summary>
/// The <c>Comment</c> entity in the <c>d</c> subgraph
/// (<c>@key(fields: "id")</c>). The <c>authorId</c> field is
/// <c>@external</c>, populated by the federation external setter
/// when the gateway resolves the <c>@requires</c> dependency.
/// </summary>
public sealed class Comment
{
    public string Id { get; set; } = default!;

    public string? Date { get; set; }

    public string? AuthorId { get; set; }
}
