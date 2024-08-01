namespace HotChocolate.Fusion.Composition.Features;

/// <summary>
/// Specifies if the fusion graph shall re-encode the ids of subgraph schemas.
/// </summary>
public sealed class ReEncodeIdsFeature : IFusionFeature
{
    private ReEncodeIdsFeature() { }

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    internal static ReEncodeIdsFeature Instance { get; } = new();
}
