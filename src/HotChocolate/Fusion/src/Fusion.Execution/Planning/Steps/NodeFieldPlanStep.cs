using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public record NodeFieldPlanStep : PlanStep
{
    public required string ResponseName { get; init; }

    public required IValueNode IdValue { get; init; }

    public bool IsPlural { get; init; }

    public required ExecutionNodeCondition[] Conditions { get; init; }

    public required OperationPlanStep FallbackQuery { get; init; }

    /// <summary>
    /// Indicates that the node is resolved by a source schema: the fallback query resolves the
    /// concrete type and the per-type branches enrich from it, so the branches run after the
    /// fallback query rather than being selected as exclusive alternatives.
    /// </summary>
    public bool SourceSchemaResolution { get; init; }

    public ImmutableDictionary<string, OperationPlanStep> Branches { get; set; }
#if NET10_0_OR_GREATER
        = [];
#else
        = ImmutableDictionary<string, OperationPlanStep>.Empty;
#endif
}
