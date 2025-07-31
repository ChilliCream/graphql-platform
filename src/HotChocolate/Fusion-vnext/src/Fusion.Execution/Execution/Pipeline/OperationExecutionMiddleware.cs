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
            if (context.VariableValues.Length > 1)
            {
                context.Result =
                    OperationResultBuilder.CreateError(
                        ErrorBuilder.New()
                            .SetMessage("Variable batching is not supported for subscriptions.")
                            .Build());
                return;
            }

            context.Result = await _planExecutor.SubscribeAsync(context, operationPlan, cancellationToken);
        }
        else
        {
            if (context.VariableValues.Length > 1)
            {
                var variableValues = ImmutableCollectionsMarshal.AsArray(context.VariableValues).AsSpan();
                var tasks = new Task<IExecutionResult>[variableValues.Length];

                for (var i = 0; i < variableValues.Length; i++)
                {
                    tasks[i] = _planExecutor.ExecuteAsync(context, variableValues[i], operationPlan, cancellationToken);
                }

                var results = await Task.WhenAll(tasks);
                context.Result = new OperationResultBatch(results);
            }
            else
            {
                context.Result = await _planExecutor.ExecuteAsync(context, context.VariableValues[0], operationPlan, cancellationToken);
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
