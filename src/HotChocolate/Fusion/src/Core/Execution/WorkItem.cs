using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal struct WorkItem
{
    public WorkItem(
        ISelectionSet selectionSet,
        ObjectResult result, 
        IReadOnlyList<string> exportKeys)
    {
        SelectionSet = selectionSet;
        Result = result;
        SelectionResults = new SelectionResult[selectionSet.Selections.Count];
        ExportKeys = exportKeys;
    }

    public Dictionary<string, IValueNode> VariableValues { get; } = new();

    public IReadOnlyList<string> ExportKeys { get; }

    public ISelectionSet SelectionSet { get; }

    public SelectionResult[] SelectionResults { get; }

    public ObjectResult Result { get; }
}
