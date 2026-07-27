namespace HotChocolate.Fusion.Suites.OverrideTypeInterface.A;

/// <summary>
/// The <c>ImagePost</c> entity as projected by subgraph <c>a</c>.
/// </summary>
public sealed class ImagePost : IPost
{
    public string Id { get; init; } = string.Empty;

    public string CreatedAt { get; init; } = string.Empty;
}
