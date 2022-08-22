using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal struct WorkItem
{
    public WorkItem(
        ISelectionSet selectionSet,
        ObjectResult result)
    {
        SelectionSet = selectionSet;
        Result = result;
        SelectionResults = new SelectionResult[selectionSet.Selections.Count];
        ExportKeys = Array.Empty<string>();
    }

    public Dictionary<string, IValueNode> VariableValues { get; } = new();

    public IReadOnlyList<string> ExportKeys { get; set; }

    public ISelectionSet SelectionSet { get; }

    public SelectionResult[] SelectionResults { get; }

    public ObjectResult Result { get; }
}
