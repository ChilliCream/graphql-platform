using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Execution;

internal struct WorkItem
{
    public WorkItem(
        ISelectionSet selectionSet,
        ObjectResult result)
        : this(Array.Empty<Argument>(), selectionSet, result) { }

    public WorkItem(
        IReadOnlyList<Argument> arguments,
        ISelectionSet selectionSet,
        ObjectResult result)
    {
        Arguments = arguments;
        SelectionSet = selectionSet;
        SelectionResults = Array.Empty<SelectionResult>();
        Result = result;
        SelectionResults = new SelectionResult[selectionSet.Selections.Count];
    }

    public IReadOnlyList<Argument> Arguments { get; }

    public ISelectionSet SelectionSet { get; }

    public SelectionResult[] SelectionResults { get; }

    public ObjectResult Result { get; }
}
