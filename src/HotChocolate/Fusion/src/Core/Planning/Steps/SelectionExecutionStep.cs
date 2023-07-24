using System.Diagnostics;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Represents the default execution step within the execution plan while
/// being in the planing phase.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal sealed class SelectionExecutionStep : ExecutionStep
{
    /// <summary>
    /// Initializes a new instance of <see cref="SelectionExecutionStep"/>.
    /// </summary>
    /// <param name="id">
    /// The id of the execution step.
    /// </param>
    /// <param name="subgraphName">
    /// The name of the subgraph from which this execution step will fetch data.
    /// </param>
    /// <param name="selectionSet">
    /// The selection set that is part of this execution step.
    /// </param>
    /// <param name="selectionSetTypeMetadata">
    /// The type metadata of the selection set.
    /// </param>
    public SelectionExecutionStep(
        int id,
        string subgraphName,
        IObjectType selectionSet,
        ObjectTypeMetadata selectionSetTypeMetadata)
        : this(id, subgraphName, null, selectionSet, selectionSetTypeMetadata)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SelectionExecutionStep"/>.
    /// </summary>
    /// <param name="id">
    /// The id of the execution step.
    /// </param>
    /// <param name="subgraphName">
    /// The name of the subgraph from which this execution step will fetch data.
    /// </param>
    /// <param name="parentSelection">
    /// The parent selection of this execution step.
    /// </param>
    /// <param name="selectionSet">
    /// The selection set that is part of this execution step.
    /// </param>
    /// <param name="selectionSetTypeMetadata">
    /// The type metadata of the selection set.
    /// </param>
    public SelectionExecutionStep(
        int id,
        string subgraphName,
        ISelection? parentSelection,
        IObjectType selectionSet,
        ObjectTypeMetadata selectionSetTypeMetadata)
        : base(id, parentSelection, selectionSet, selectionSetTypeMetadata)
    {
        SubgraphName = subgraphName;
    }

    /// <summary>
    /// Gets the subgraph from which this execution step will fetch data.
    /// </summary>
    public string SubgraphName { get; }

    /// <summary>
    /// Gets the resolver for this execution step.
    /// </summary>
    public ResolverDefinition? Resolver { get; set; }

    /// <summary>
    /// Gets the root selections of this execution step.
    /// </summary>
    public List<RootSelection> RootSelections { get; } = new();

    /// <summary>
    /// Gets all selections that are part of this execution step.
    /// </summary>
    public HashSet<ISelection> AllSelections { get; } = new();

    /// <summary>
    /// Gets all selection sets that are part of this execution step.
    /// </summary>
    public HashSet<ISelectionSet> AllSelectionSets { get; } = new();

    /// <summary>
    /// Gets the selection resolvers.
    /// </summary>
    public Dictionary<ISelection, ResolverDefinition> SelectionResolvers { get; } = new();

    /// <summary>
    /// Gets a map for this execution task from the variable name
    /// to the internal state key.
    /// </summary>
    public Dictionary<string, string> Variables { get; } = new();

    /// <summary>
    /// Gets the argument target types.
    /// </summary>
    public Dictionary<string, ITypeNode> ArgumentTypes { get; } = new();

    /// <summary>
    /// The variable requirements by this task.
    /// </summary>
    public HashSet<string> Requires { get; } = new();

    private string GetDebuggerDisplay()
    {
        var displayName = $"{Id} {SubgraphName}.{SelectionSetType.Name}";

        if (DependsOn.Count > 0)
        {
            displayName = $"{displayName} dependsOn: {string.Join(", ", DependsOn.Select(t => t.Id))}";
        }

        if(RootSelections.Count > 0)
        {
            var rootSelections = string.Join(", ", RootSelections.Select(t => t.Selection.ResponseName));
            displayName = $"{displayName} roots: {rootSelections}";
        }

        return displayName;
    }
}
