using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationExecutionMiddleware
{
    private readonly QueryExecutor _queryExecutor = new();

    public async ValueTask InvokeAsync(
        RequestContext context,
        ResultPoolSession resultPoolSession,
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
            context,
            resultPoolSession);

        context.Result = await _queryExecutor.QueryAsync(operationPlanContext, cancellationToken);

        await next(context);
    }

    public static RequestMiddlewareConfiguration Create()
    {
        return new RequestMiddlewareConfiguration(
            (_, next) =>
            {
                var middleware = new OperationExecutionMiddleware();
                return context => middleware.InvokeAsync(
                    context,
                    context.RequestServices.GetRequiredService<ResultPoolSession>(),
                    next,
                    context.RequestAborted);
            },
            nameof(OperationExecutionMiddleware));
    }
}
