using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationExecutionMiddleware
{
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

        // the incremental delivery check depends on the accepted response content types alone
        // and runs before the operation kind check.
        if (!IsIncrementalDeliveryAllowed(operationPlan, context.Request))
        {
            context.Result = ErrorHelper.IncrementalDeliveryNotAcceptable();
            return;
        }

        var operation = operationPlan.Operation;

        if (!IsOperationKindAllowed(operation, context.Request))
        {
            context.Result = ErrorHelper.OperationKindNotAllowed(GetRequiredFlag(operation));
            return;
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

                context.Result = await OperationPlanExecutor.SubscribeAsync(context, operationPlan, cancellationToken);
            }
            else
            {
                if (context.VariableValues.Length > 1)
                {
                    if (!operationPlan.IncrementalPlans.IsEmpty)
                    {
                        var error = ErrorBuilder.New()
                            .SetMessage("Variable batching is not supported with @defer.")
                            .Build();

                        _diagnosticEvents.RequestError(context, error);

                        context.Result = OperationResult.FromError(error);
                        return;
                    }

                    var variableValues = ImmutableCollectionsMarshal.AsArray(context.VariableValues).AsSpan();
                    var tasks = new Task<IExecutionResult>[variableValues.Length];

                    for (var i = 0; i < variableValues.Length; i++)
                    {
                        tasks[i] = OperationPlanExecutor.ExecuteAsync(
                            context,
                            variableValues[i],
                            operationPlan,
                            cancellationToken);
                    }

                    var results = ImmutableList.CreateRange(await Task.WhenAll(tasks));
                    context.Result = new OperationResultBatch(results);
                }
                else if (!operationPlan.IncrementalPlans.IsEmpty)
                {
                    context.Result = await OperationPlanExecutor.ExecuteWithDeferAsync(
                        context,
                        context.VariableValues[0],
                        operationPlan,
                        cancellationToken);
                }
                else
                {
                    context.Result = await OperationPlanExecutor.ExecuteAsync(
                        context,
                        context.VariableValues[0],
                        operationPlan,
                        cancellationToken);
                }
            }
        }

        await next(context);
    }

    private static bool IsIncrementalDeliveryAllowed(
        OperationPlan operationPlan,
        IOperationRequest request)
    {
        if (request.Flags is RequestFlags.AllowAll || operationPlan.IncrementalPlans.IsEmpty)
        {
            return true;
        }

        return (request.Flags & RequestFlags.AllowStreams) == RequestFlags.AllowStreams;
    }

    private static bool IsOperationKindAllowed(Operation operation, IOperationRequest request)
    {
        if (request.Flags is RequestFlags.AllowAll)
        {
            return true;
        }

        var requiredFlag = GetRequiredFlag(operation);

        return requiredFlag is RequestFlags.None || (request.Flags & requiredFlag) == requiredFlag;
    }

    private static RequestFlags GetRequiredFlag(Operation operation)
        => operation.Definition.Operation switch
        {
            OperationType.Query => RequestFlags.AllowQuery,
            OperationType.Mutation => RequestFlags.AllowMutation,
            OperationType.Subscription => RequestFlags.AllowSubscription,
            _ => RequestFlags.None
        };

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
