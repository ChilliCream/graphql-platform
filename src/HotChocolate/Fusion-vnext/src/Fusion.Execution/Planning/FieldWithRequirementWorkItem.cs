using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types.Metadata;

namespace HotChocolate.Fusion.Planning;

internal sealed record FieldRequirementWorkItem(
    FieldSelection Selection,
    int StepId,
    Lookup? Lookup = null)
    : WorkItem
{
    public ExecutionNodeCondition[] Conditions { get; init; } = [];

    public int StepIndex => StepId - 1;

    public override double Cost => Lookup is null ? 1 : 2;
}
