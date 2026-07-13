using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Planning;

public sealed record PolicyPlanStep : PlanStep
{
    public required ImmutableArray<PolicyExecutionTarget> Targets { get; init; }

    public ExecutionNodeCondition[] Conditions { get; init; } = [];
}
