using System.Diagnostics.Contracts;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Represents the default execution step within the execution plan while
/// being in the planing phase.
/// </summary>
internal sealed class SelectionExecutionStep : ExecutionStep
{
    public SelectionExecutionStep(
        string subgraphName,
        ObjectTypeInfo selectionSetTypeInfo,
        ISelection? parentSelection)
        : base(selectionSetTypeInfo, parentSelection)
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
    /// Gets a map for this execution task from the variable name
    /// to the internal state key.
    /// </summary>
    public Dictionary<string, string> Variables { get; } = new();

    /// <summary>
    /// The variable requirements by this task.
    /// </summary>
    public HashSet<string> Requires { get; } = new();
}
