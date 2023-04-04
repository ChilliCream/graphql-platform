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
        ObjectResult result,
        IReadOnlyList<string> exportKeys)
    {
        SelectionSet = selectionSet;
        Result = result;
        SelectionResults = new SelectionData[selectionSet.Selections.Count];
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
    /// The selection set that is being executed.
    /// </summary>
    public ISelectionSet SelectionSet { get; }

    /// <summary>
    /// The selection results that are being collected.
    /// </summary>
    public SelectionData[] SelectionResults { get; }

    public ObjectResult Result { get; }
}
