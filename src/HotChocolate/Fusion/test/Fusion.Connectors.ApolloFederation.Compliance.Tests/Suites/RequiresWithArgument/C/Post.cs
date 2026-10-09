namespace HotChocolate.Fusion.Suites.RequiresWithArgument.C;

/// <summary>
/// The <c>Post</c> entity owned by the <c>c</c> subgraph
/// (<c>@key(fields: "id")</c>).
/// </summary>
public sealed class Post
{
    public string Id { get; init; } = default!;
}
