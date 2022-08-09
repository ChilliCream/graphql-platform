using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Execution;

internal struct WorkItem
{
    public WorkItem(
        ISelectionSet selectionSet,
        ArgumentContext variables,
        ObjectResult result)
        : this(Array.Empty<Argument>(), selectionSet, variables, result) { }

    public WorkItem(
        IReadOnlyList<Argument> arguments,
        ISelectionSet selectionSet,
        ArgumentContext variables,
        ObjectResult result)
    {
        Arguments = arguments;
        SelectionSet = selectionSet;
        SelectionResults = Array.Empty<SelectionResult>();
        Variables = variables;
        Result = result;
    }

    public IReadOnlyList<Argument> Arguments { get; }

    public ISelectionSet SelectionSet { get; }

    public IReadOnlyList<SelectionResult> SelectionResults { get; set; }

    public ArgumentContext Variables { get; set; }

    public ObjectResult Result { get; }
}
