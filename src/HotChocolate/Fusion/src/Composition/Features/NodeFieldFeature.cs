namespace HotChocolate.Fusion.Composition.Features;

public sealed class NodeFieldFeature : IFusionFeature
{
    private NodeFieldFeature() { }

    internal static NodeFieldFeature Instance { get; } = new();
}