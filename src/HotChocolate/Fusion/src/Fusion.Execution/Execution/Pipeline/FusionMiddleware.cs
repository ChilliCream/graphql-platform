using HotChocolate.Execution;
using HotChocolate.Execution.Pipeline;

namespace HotChocolate.Fusion.Execution.Pipeline;

/// <summary>
/// Provides the fusion middleware configurations.
/// </summary>
public static class FusionMiddleware
{
    public static RequestMiddlewareConfiguration OperationExecution
        => OperationExecutionMiddleware.Create();

    public static RequestMiddlewareConfiguration OperationPlanCache
        => OperationPlanCacheMiddleware.Create();

    public static RequestMiddlewareConfiguration OperationPlan
        => OperationPlanMiddleware.Create();

    public static RequestMiddlewareConfiguration OperationVariableCoercion
        => OperationVariableCoercionMiddleware.Create();

    public static RequestMiddlewareConfiguration Timeout
        => TimeoutMiddleware.Create();

    public static RequestMiddlewareConfiguration ConcurrencyGate
        => CommonMiddleware.ConcurrencyGate;
}
