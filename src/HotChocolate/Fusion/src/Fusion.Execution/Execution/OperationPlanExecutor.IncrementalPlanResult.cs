using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution;

internal static partial class OperationPlanExecutor
{
    private readonly record struct IncrementalPlanResult(
        IncrementalPlan? IncrementalPlan,
        ImmutableArray<DeliveryGroup> DeliveryGroups,
        OperationPlanContext? Context,
        OperationResult? Result,
        Exception? Error);
}
