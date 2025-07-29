using System.Runtime.InteropServices;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationExecutionMiddleware
{
    private readonly OperationPlanExecutor _planExecutor = new();

    public async ValueTask InvokeAsync(
        RequestContext context,
        ResultPoolSession resultPoolSession,
        RequestDelegate next,
        CancellationToken cancellationToken)
    {
        var operationPlan = context.GetOperationPlan();

        if (operationPlan is null)
        {
            throw new InvalidOperationException(
                "There is no operation plan available to be executed.");
        }

        if (operationPlan.Operation.Definition.Operation is OperationType.Subscription)
        {
            // todo : implement
            throw new NotSupportedException("Not yet supported.");
        }
        else
        {
            if (context.VariableValues.Length > 1)
            {
                var variableValues = ImmutableCollectionsMarshal.AsArray(context.VariableValues).AsSpan();
                var tasks = new Task<IExecutionResult>[variableValues.Length];

                for (var i = 0; i < variableValues.Length; i++)
                {
                    var planContext = new OperationPlanContext(
                        operationPlan,
                        variableValues[i],
                        context,
                        resultPoolSession);

                    tasks[i] = _planExecutor.ExecuteAsync(planContext, cancellationToken);
                }

                var results = await Task.WhenAll(tasks).ConfigureAwait(false);
                context.Result = new OperationResultBatch(results);
            }
            else
            {
                var planContext = new OperationPlanContext(
                    operationPlan,
                    context.VariableValues[0],
                    context,
                    resultPoolSession);

                context.Result = await _planExecutor.ExecuteAsync(planContext, cancellationToken);
            }
        }

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
