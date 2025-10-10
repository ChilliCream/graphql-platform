using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public record NodeFieldPlanStep : PlanStep
{
    public required string ResponseName { get; init; }

    public required IValueNode IdValue { get; init; }

    public required ExecutionNodeCondition[] Conditions { get; init; }

    public required OperationPlanStep FallbackQuery { get; init; }

    public ImmutableDictionary<string, OperationPlanStep> Branches { get; set; }
#if NET10_0_OR_GREATER
        = [];
#else
        = ImmutableDictionary<string, OperationPlanStep>.Empty;
#endif
}
