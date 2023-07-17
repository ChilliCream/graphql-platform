namespace HotChocolate.Fusion.Composition.Features;

public sealed class ReEncodeIdsFeature : IFusionFeature
{
    private ReEncodeIdsFeature() { }

    internal static ReEncodeIdsFeature Instance { get; } = new();
}