using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationExecutionMiddleware
{
    private readonly OperationPlanExecutor _planExecutor = new();
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;

    private OperationExecutionMiddleware(IFusionExecutionDiagnosticEvents diagnosticEvents)
    {
        _diagnosticEvents = diagnosticEvents;
    }

    public async ValueTask InvokeAsync(
        RequestContext context,
        RequestDelegate next,
        CancellationToken cancellationToken)
    {
        var operationPlan = context.GetOperationPlan();

        if (operationPlan is null)
        {
            throw new InvalidOperationException(
                "There is no operation plan available to be executed.");
        }

        using (_diagnosticEvents.ExecuteOperation(context))
        {
            if (operationPlan.Operation.Definition.Operation is OperationType.Subscription)
            {
                if (context.VariableValues.Length > 1)
                {
                    var error = ErrorBuilder.New()
                        .SetMessage("Variable batching is not supported for subscriptions.")
                        .Build();

                    _diagnosticEvents.RequestError(context, error);

                    context.Result = OperationResult.FromError(error);
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
                        tasks[i] = _planExecutor.ExecuteAsync(
                            context,
                            variableValues[i],
                            operationPlan,
                            cancellationToken);
                    }

                    var results = ImmutableList.CreateRange(await Task.WhenAll(tasks));
                    context.Result = new OperationResultBatch(results);
                }
                else
                {
                    context.Result = await _planExecutor.ExecuteAsync(
                        context,
                        context.VariableValues[0],
                        operationPlan,
                        cancellationToken);
                }
            }
        }

        await next(context);
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (fc, next) =>
            {
                var diagnosticEvents = fc.SchemaServices.GetRequiredService<IFusionExecutionDiagnosticEvents>();
                var middleware = new OperationExecutionMiddleware(diagnosticEvents);
                return context => middleware.InvokeAsync(
                    context,
                    next,
                    context.RequestAborted);
            },
            WellKnownRequestMiddleware.OperationExecutionMiddleware);
}
