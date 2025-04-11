using System.Collections.Immutable;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public abstract record WorkItem
{
    public ImmutableHashSet<int> Dependents { get; init; } = [];
}

public record OperationWorkItem(
    OperationWorkItemKind Kind,
    SelectionSet SelectionSet,
    Lookup? Lookup = null)
    : WorkItem
{
    public static OperationWorkItem CreateRoot(SelectionSet selectionSet) =>
        new(OperationWorkItemKind.Root, selectionSet);
}

public sealed record FieldWithRequirementWorkItem(
    FieldSelection Selection,
    int StepId,
    Lookup? Lookup = null)
    : WorkItem
{
    public int StepIndex => StepId - 1;
}

public record FieldRequirementsWorkItem(
    string Key,
    int StepId,
    SelectionSet SelectionSet,
    Lookup? Lookup = null)
    : WorkItem;
