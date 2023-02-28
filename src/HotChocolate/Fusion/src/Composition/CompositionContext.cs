using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

public sealed class CompositionContext
{
    public CompositionContext(IReadOnlyList<SubGraphConfiguration> configurations)
    {
        Configurations = configurations;
    }

    public IReadOnlyList<SubGraphConfiguration> Configurations { get; }

    public List<Schema> SubGraphs { get; } = new();

    public List<EntityGroup> Entities { get; } = new();

    public Schema FusionGraph { get; } = new();

    public CancellationToken Abort { get; set; }

    public ICompositionLog Log { get; } = new CompositionLog();
}
