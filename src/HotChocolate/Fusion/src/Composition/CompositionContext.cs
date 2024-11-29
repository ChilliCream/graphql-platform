using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// The context that is available during composition.
/// </summary>
internal sealed class CompositionContext
{
    private static readonly HashSet<SchemaCoordinate> _empty = [];
    private readonly Dictionary<string, HashSet<SchemaCoordinate>> _taggedTypes =
        new(StringComparer.Ordinal);

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
    /// <param name="features">
    /// The composition features.
    /// </param>
    /// <param name="log">
    /// The composition log.
    /// </param>
    public CompositionContext(
        IReadOnlyList<SubgraphConfiguration> configurations,
        FusionFeatureCollection features,
        ICompositionLog log,
        string? fusionTypePrefix = null,
        bool fusionTypeSelf = false)
    {
        Configurations = configurations;
        Features = features;
        FusionGraph = new();
        FusionTypes = new FusionTypes(FusionGraph, fusionTypePrefix, fusionTypeSelf);
        Log = log;
    }

    /// <summary>
    /// Gets the subgraph configurations.
    /// </summary>
    public IReadOnlyList<SubgraphConfiguration> Configurations { get; }

    /// <summary>
    /// Gets the composition features.
    /// </summary>
    public FusionFeatureCollection Features { get; }

    /// <summary>
    /// Gets the subgraph schemas.
    /// </summary>
    public List<SchemaDefinition> Subgraphs { get; } = [];

    /// <summary>
    /// Get the grouped subgraph entities.
    /// </summary>
    public List<EntityGroup> Entities { get; } = [];

    /// <summary>
    /// Gets the fusion graph schema.
    /// </summary>
    public SchemaDefinition FusionGraph { get; }

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
    public ICompositionLog Log { get; }

    /// <summary>
    /// Gets a set that can be used to calculate subgraph support of a component.
    /// </summary>
    public HashSet<string> SupportedBy { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a map that can be used to store custom context data.
    /// </summary>
    public Dictionary<string, object?> ContextData { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the subgraph schema by its name.
    /// </summary>
    /// <param name="subgraphName">
    /// The name of the subgraph.
    /// </param>
    /// <returns>
    /// Returns the subgraph schema.
    /// </returns>
    public SchemaDefinition GetSubgraphSchema(string subgraphName)
        => Subgraphs.First(t => t.Name.EqualsOrdinal(subgraphName));

    /// <summary>
    /// Tries to resolve a type system member from the specified subgraph by its schema coordinate.
    /// </summary>
    /// <param name="subgraphName">
    /// The name of the subgraph.
    /// </param>
    /// <param name="coordinate">
    /// The schema coordinate.
    /// </param>
    /// <param name="member">
    /// The resolved type system member.
    /// </param>
    /// <typeparam name="T">
    /// The type of the type system member.
    /// </typeparam>
    /// <returns>
    /// <c>true</c> if the type system member was resolved; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetSubgraphMember<T>(
        string subgraphName,
        SchemaCoordinate coordinate,
        [NotNullWhen(true)] out T? member)
        where T : ITypeSystemMemberDefinition
        => GetSubgraphSchema(subgraphName).TryGetMember(coordinate, out member);

    public IEnumerable<T> GetSubgraphMembers<T>(SchemaCoordinate coordinate)
        where T : ITypeSystemMemberDefinition
    {
        foreach (var subgraph in Subgraphs)
        {
            if (subgraph.TryGetMember(coordinate, out var result))
            {
                yield return (T)result;
            }
        }
    }
}
