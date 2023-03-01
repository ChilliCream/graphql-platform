using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

public sealed class CompositionContext
{
    public CompositionContext(
        IReadOnlyList<SubGraphConfiguration> configurations,
        string? fusionTypePrefix = null)
    {
        Configurations = configurations;
        FusionGraph = new();
        FusionTypes = new FusionTypes(FusionGraph, fusionTypePrefix);
    }

    public IReadOnlyList<SubGraphConfiguration> Configurations { get; }

    public List<Schema> SubGraphs { get; } = new();

    public List<EntityGroup> Entities { get; } = new();

    public Schema FusionGraph { get; }

    public FusionTypes FusionTypes { get; }

    public CancellationToken Abort { get; set; }

    public ICompositionLog Log { get; } = new CompositionLog();
}
