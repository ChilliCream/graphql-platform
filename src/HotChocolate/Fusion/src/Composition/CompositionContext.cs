using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// The context that is available during composition.
/// </summary>
internal sealed class CompositionContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="CompositionContext"/>.
    /// </summary>
    /// <param name="configurations">
    /// The sub-graph configurations.
    /// </param>
    /// <param name="fusionTypePrefix">
    /// The prefix that is used for the fusion types.
    /// </param>
    /// <param name="fusionTypeSelf">
    /// Defines if the fusion types should be prefixed with the sub-graph name.
    /// </param>
    public CompositionContext(
        IReadOnlyList<SubGraphConfiguration> configurations,
        string? fusionTypePrefix = null,
        bool fusionTypeSelf = false)
    {
        Configurations = configurations;
        FusionGraph = new();
        FusionTypes = new FusionTypes(FusionGraph, fusionTypePrefix, fusionTypeSelf);
    }

    /// <summary>
    /// Gets the sub-graph configurations.
    /// </summary>
    public IReadOnlyList<SubGraphConfiguration> Configurations { get; }

    /// <summary>
    /// Gets the sub-graph schemas.
    /// </summary>
    public List<Schema> SubGraphs { get; } = new();

    /// <summary>
    /// Get the grouped sub-graph entities.
    /// </summary>
    public List<EntityGroup> Entities { get; } = new();

    /// <summary>
    /// Gets the fusion graph schema.
    /// </summary>
    public Schema FusionGraph { get; }

    /// <summary>
    /// Gets the fusion types.
    /// </summary>
    public FusionTypes FusionTypes { get; }

    /// <summary>
    /// Gets or sets a cancellation token that can be used to abort composition.
    /// </summary>
    public CancellationToken Abort { get; set; }

    /// <summary>
    /// Gets the composition log.
    /// </summary>
    public ICompositionLog Log { get; } = new DefaultCompositionLog();
}
