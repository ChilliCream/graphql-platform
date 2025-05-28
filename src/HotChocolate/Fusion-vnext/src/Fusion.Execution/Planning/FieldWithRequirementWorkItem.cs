using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public sealed record FieldRequirementWorkItem(
    FieldSelection Selection,
    int StepId,
    Lookup? Lookup = null)
    : WorkItem
{
    public int StepIndex => StepId - 1;
}
