using System.Diagnostics.CodeAnalysis;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;

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
    /// <param name="log">
    /// The composition log.
    /// </param>
    public CompositionContext(
        IReadOnlyList<SubgraphConfiguration> configurations,
        ICompositionLog log,
        string? fusionTypePrefix = null,
        bool fusionTypeSelf = false)
    {
        Configurations = configurations;
        FusionGraph = new();
        FusionTypes = new FusionTypes(FusionGraph, fusionTypePrefix, fusionTypeSelf);
        Log = log;
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
    /// Gets or sets the composition feature flags.
    /// </summary>
    public FusionFeatureFlags Features { get; set; }

    /// <summary>
    /// Gets or sets a cancellation token that can be used to abort composition.
    /// </summary>
    public CancellationToken Abort { get; set; }

    /// <summary>
    /// Gets the composition log.
    /// </summary>
    public ICompositionLog Log { get; }

    public bool TryGetSubgraphMember<T>(
        string subgraphName,
        SchemaCoordinate coordinate,
        [NotNullWhen(true)] out T? member)
        where T : ITypeSystemMember
    {
        var subgraph = Subgraphs.First(t => t.Name.EqualsOrdinal(subgraphName));
        return subgraph.TryGetMember(coordinate, out member);
    }
}
