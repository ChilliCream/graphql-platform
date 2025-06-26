using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationExecutionMiddleware
{
    private readonly QueryExecutor _queryExecutor = new();

    public async ValueTask InvokeAsync(
        RequestContext context,
        RequestDelegate next,
        CancellationToken cancellationToken)
    {
        var operationPlan = context.GetOperationPlan();

        if (operationPlan is null)
        {
            throw new InvalidOperationException();
        }

        var operationPlanContext = new OperationPlanContext(
            operationPlan,
            context.VariableValues[0],
            context);

        context.Result = await _queryExecutor.QueryAsync(operationPlanContext, cancellationToken);

        await next(context);
    }

    public static RequestMiddlewareConfiguration Create()
    {
        return new RequestMiddlewareConfiguration(
            (fc, next) =>
            {
                var middleware = new OperationExecutionMiddleware();
                return context => middleware.InvokeAsync(context, next, context.RequestAborted);
            },
            nameof(OperationExecutionMiddleware));
    }
}
