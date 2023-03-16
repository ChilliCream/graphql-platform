using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Represents the default execution step within the execution plan while
/// being in the planing phase.
/// </summary>
internal sealed class DefaultExecutionStep : ExecutionStep
{
    public DefaultExecutionStep(
        string subgraphName,
        ObjectType selectionSetType,
        ISelection? parentSelection)
        : base(selectionSetType, parentSelection)
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

    public HashSet<ISelection> AllSelections { get; } = new();

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
