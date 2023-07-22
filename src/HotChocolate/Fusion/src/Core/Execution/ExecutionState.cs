using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents the query plan state for a single selection set.
/// This working state can be shared between nodes of a query plan.
/// </summary>
internal sealed class ExecutionState
{
    public ExecutionState(
        SelectionSet selectionSet,
        ObjectResult selectionSetResult,
        IReadOnlyList<string> provides)
    {
        SelectionSet = selectionSet;
        SelectionSetResult = selectionSetResult;
        SelectionSetData = new SelectionData[selectionSet.Selections.Count];
        Provides = provides;
    }

    /// <summary>
    /// Gets a collection of exported variable values that
    /// are passed on to the next query plan node.
    /// </summary>
    public Dictionary<string, IValueNode> VariableValues { get; } = new();

    /// <summary>
    /// Gets a list of keys representing the state that is being
    /// provided after the associated <see cref="SelectionSet"/>
    /// has been executed.
    /// </summary>
    public IReadOnlyList<string> Provides { get; }

    /// <summary>
    /// Gets the selection set that is being executed.
    /// </summary>
    public SelectionSet SelectionSet { get; }

    /// <summary>
    /// Gets the selection set data that was collected during execution.
    /// </summary>
    public SelectionData[] SelectionSetData { get; }

    /// <summary>
    /// Gets the completed selection set result.
    /// </summary>
    public ObjectResult SelectionSetResult { get; }

    /// <summary>
    /// Gets a flag that indicates if the work item has been initialized.
    /// </summary>
    public bool IsInitialized { get; set; }
}
