using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Pipeline;

/// <summary>
/// Provides the fusion middleware configurations.
/// </summary>
public static class FusionMiddleware
{
    public static string OperationExecutionKey
        => nameof(OperationExecutionMiddleware);

    public static RequestMiddlewareConfiguration OperationExecution
        => OperationExecutionMiddleware.Create();

    public static string OperationPlanCacheKey
        => nameof(OperationPlanCacheMiddleware);

    public static RequestMiddlewareConfiguration OperationPlanCache
        => OperationPlanCacheMiddleware.Create();

    public static string OperationPlanKey
        => nameof(OperationPlanMiddleware);

    public static RequestMiddlewareConfiguration OperationPlan
        => OperationPlanMiddleware.Create();

    public static string OperationVariableCoercionKey
        => nameof(OperationVariableCoercionMiddleware);

    public static RequestMiddlewareConfiguration OperationVariableCoercion
        => OperationVariableCoercionMiddleware.Create();

    public static string TimeoutKey
        => nameof(TimeoutMiddleware);

    public static RequestMiddlewareConfiguration Timeout
        => TimeoutMiddleware.Create();
}
