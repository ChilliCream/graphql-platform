namespace HotChocolate.Fusion.Suites.SimpleOverride.B;

/// <summary>
/// The <c>Post</c> entity as projected by the <c>b</c> subgraph.
/// </summary>
public sealed class Post
{
    public string? Id { get; init; }

    public string? CreatedAt { get; init; }
}
