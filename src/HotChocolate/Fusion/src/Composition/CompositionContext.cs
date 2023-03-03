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
    /// The subgraph configurations.
    /// </param>
    /// <param name="fusionTypePrefix">
    /// The prefix that is used for the fusion types.
    /// </param>
    /// <param name="fusionTypeSelf">
    /// Defines if the fusion types should be prefixed with the subgraph name.
    /// </param>
    public CompositionContext(
        IReadOnlyList<SubgraphConfiguration> configurations,
        string? fusionTypePrefix = null,
        bool fusionTypeSelf = false)
    {
        Configurations = configurations;
        FusionGraph = new();
        FusionTypes = new FusionTypes(FusionGraph, fusionTypePrefix, fusionTypeSelf);
    }

    /// <summary>
    /// Gets the subgraph configurations.
    /// </summary>
    public IReadOnlyList<SubgraphConfiguration> Configurations { get; }

    /// <summary>
    /// Gets the subgraph schemas.
    /// </summary>
    public List<Schema> Subgraphs { get; } = new();

    /// <summary>
    /// Get the grouped subgraph entities.
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
