using System.Collections.Immutable;
using HotChocolate.CostAnalysis;

namespace HotChocolate.Fusion.Execution.CostAnalysis;

internal enum CostLimitKind
{
    FieldCost,
    TypeCost,
    ResponseSize
}

internal sealed record CostBatchEnforcementResult(
    ImmutableArray<CostEstimate> Estimates,
    ImmutableArray<CostLimitViolation?> Violations);

internal readonly record struct CostLimitViolation(CostLimitKind Kind, double Limit);
