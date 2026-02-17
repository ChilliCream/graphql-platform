using HotChocolate.Fusion.Types.Metadata;

namespace HotChocolate.Fusion.Planning;

internal sealed record OperationWorkItem(
    OperationWorkItemKind Kind,
    SelectionSet SelectionSet,
    Lookup? Lookup = null,
    string? FromSchema = null)
    : WorkItem
{
    public override int EstimatedDepth
        => Kind is OperationWorkItemKind.Root
            ? 1
            : base.EstimatedDepth;

    public static OperationWorkItem CreateRoot(SelectionSet selectionSet)
        => new(OperationWorkItemKind.Root, selectionSet);
}
