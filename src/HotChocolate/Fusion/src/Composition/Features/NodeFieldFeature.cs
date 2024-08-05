namespace HotChocolate.Fusion.Composition.Features;

/// <summary>
/// Specifies if the fusion graph shall support the global identification spec.
/// </summary>
public sealed class NodeFieldFeature : IFusionFeature
{
    private NodeFieldFeature() { }

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    internal static NodeFieldFeature Instance { get; } = new();
}
