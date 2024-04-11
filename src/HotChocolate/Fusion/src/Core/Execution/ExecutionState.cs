using System.Diagnostics;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents the query plan state for a single selection set.
/// This working state can be shared between nodes of a query plan.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal sealed class ExecutionState
{
    public ExecutionState(
        SelectionSet selectionSet,
        ObjectResult selectionSetResult,
        IReadOnlyList<string> requires)
    {
        SelectionSet = selectionSet;
        SelectionSetResult = selectionSetResult;
        SelectionSetData = new SelectionData[selectionSet.Selections.Count];
        Requires = requires;
    }

    /// <summary>
    /// Gets a collection of exported variable values that
    /// are passed on to the next query plan node.
    /// </summary>
    public Dictionary<string, IValueNode> VariableValues { get; } = new();

    /// <summary>
    /// Gets a list of keys representing the state that is being
    /// required to fetch data for the associated <see cref="SelectionSet"/>.
    /// </summary>
    public IReadOnlyList<string> Requires { get; }

    /// <summary>
    /// Gets the selection set that is being executed.
    /// </summary>
    public SelectionSet SelectionSet { get; }

    /// <summary>
    /// Gets the selection set data that was collected during execution.
    /// The selection set data represents the data that we have collected
    /// from the subgraphs for the <see cref="SelectionSet"/>.
    /// </summary>
    public SelectionData[] SelectionSetData { get; }

    /// <summary>
    /// Gets the completed selection set result.
    /// The selection set result represents the data for the
    /// <see cref="SelectionSet"/> that we deliver to the user.
    /// </summary>
    public ObjectResult SelectionSetResult { get; }

    /// <summary>
    /// Gets a flag that indicates if the work item has been initialized.
    /// </summary>
    public bool IsInitialized { get; set; }

    private string GetDebuggerDisplay()
    {
        var displayName = $"State {SelectionSet.Id}";

        if (Requires.Count > 0)
        {
            displayName = $"{displayName} requires: {string.Join(", ", Requires)}";
        }

        return displayName;
    }
}
