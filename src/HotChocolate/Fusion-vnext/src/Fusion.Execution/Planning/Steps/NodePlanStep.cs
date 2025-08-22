using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public record NodePlanStep : PlanStep
{
    public required string ResponseName { get; init; }

    public required IValueNode IdValue { get; init; }

    public required OperationPlanStep FallbackQuery { get; init; }

    public ImmutableDictionary<string, OperationPlanStep> Branches { get; set; }
        = ImmutableDictionary<string, OperationPlanStep>.Empty;
}
