using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types.Metadata;

namespace HotChocolate.Fusion.Planning;

internal sealed record OperationWorkItem(
    OperationWorkItemKind Kind,
    SelectionSet SelectionSet,
    Lookup? Lookup = null,
    string? FromSchema = null)
    : WorkItem
{
    public ExecutionNodeCondition[] Conditions { get; init; } = [];

    /// <summary>
    /// Indicates that this selection set may be resolved by a lookup into
    /// <see cref="FromSchema"/> itself. This is the case for selections that an event
    /// stream spilled because the message shape did not carry them, even though the
    /// source schema owns them. Normal federation never re-enters the schema a selection
    /// came from, so this stays <c>false</c> for every other flow.
    /// </summary>
    public bool AllowSourceSchemaReentry { get; init; }

    public override int EstimatedDepth
        => Kind is OperationWorkItemKind.Root
            ? 1
            : base.EstimatedDepth;

    public static OperationWorkItem CreateRoot(SelectionSet selectionSet)
        => new(OperationWorkItemKind.Root, selectionSet);
}
