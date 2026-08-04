namespace HotChocolate.Fusion.Suites.OverrideTypeInterface.B;

/// <summary>
/// The <c>TextPost</c> entity as projected by subgraph <c>b</c>.
/// </summary>
public sealed class TextPost : IPost
{
    public string Id { get; init; } = string.Empty;

    public string CreatedAt { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;
}
