using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents the working state for a single selection set.
/// This working state can be shared between query plan nodes.
/// </summary>
internal sealed class WorkItem
{
    public WorkItem(
        ISelectionSet selectionSet,
        ObjectResult selectionSetResult,
        IReadOnlyList<string> exportKeys)
    {
        SelectionSet = selectionSet;
        SelectionSetResult = selectionSetResult;
        SelectionSetData = new SelectionData[selectionSet.Selections.Count];
        ExportKeys = exportKeys;
    }

    /// <summary>
    /// Gets a collection of exported variable values that
    /// are passed on to the next query plan node.
    /// </summary>
    public Dictionary<string, IValueNode> VariableValues { get; } = new();

    /// <summary>
    /// Gets a list of keys representing the state that is being
    /// exported while processing this work item.
    /// </summary>
    public IReadOnlyList<string> ExportKeys { get; }

    /// <summary>
    /// Gets the selection set that is being executed.
    /// </summary>
    public ISelectionSet SelectionSet { get; }

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
    /// <value></value>
    public bool IsInitialized { get; set; }
}
