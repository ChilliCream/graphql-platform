using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public sealed record OperationWorkItem(
    OperationWorkItemKind Kind,
    SelectionSet SelectionSet,
    Lookup? Lookup = null,
    string? RequirementKey = null)
    : WorkItem
{
    public static OperationWorkItem CreateRoot(SelectionSet selectionSet)
        => new(OperationWorkItemKind.Root, selectionSet);
}
