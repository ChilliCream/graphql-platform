namespace HotChocolate.Fusion.Suites.OverrideTypeInterface.B;

/// <summary>
/// The <c>ImagePost</c> entity as projected by subgraph <c>b</c>. Implements
/// <c>AnotherPost</c> (not <c>Post</c>) so the override flow is anchored on
/// a separate interface.
/// </summary>
public sealed class ImagePost : IAnotherPost
{
    public string Id { get; init; } = string.Empty;

    public string CreatedAt { get; init; } = string.Empty;
}
