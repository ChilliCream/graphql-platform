using HotChocolate.Fusion.Types.Metadata;

namespace HotChocolate.Fusion.Planning;

internal sealed record OperationWorkItem(
    OperationWorkItemKind Kind,
    SelectionSet SelectionSet,
    Lookup? Lookup = null,
    string? FromSchema = null)
    : WorkItem
{
    public static OperationWorkItem CreateRoot(SelectionSet selectionSet)
        => new(OperationWorkItemKind.Root, selectionSet);
}
