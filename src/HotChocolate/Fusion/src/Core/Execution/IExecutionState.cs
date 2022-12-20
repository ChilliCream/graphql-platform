using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Execution;

internal interface IExecutionState
{
    bool ContainsState(ISelectionSet selectionSet);

    bool TryGetState(
        ISelectionSet selectionSet,
        [NotNullWhen(true)] out IReadOnlyList<WorkItem>? values);

    void RegisterState(WorkItem value);
}
