using HotChocolate.Execution;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationPlanMiddleware
{
    private readonly OperationPlanner _planner;
    private readonly IOperationPlannerInterceptor[] _interceptors;
    private readonly IFusionExecutionDiagnosticEvents _diagnosticsEvents;

    private OperationPlanMiddleware(
        OperationPlanner planner,
        IEnumerable<IOperationPlannerInterceptor>? interceptors,
        IFusionExecutionDiagnosticEvents diagnosticsEvents)
    {
        _planner = planner;
        _interceptors = interceptors?.ToArray() ?? [];
        _diagnosticsEvents = diagnosticsEvents;
    }

    public ValueTask InvokeAsync(RequestContext context, RequestDelegate next)
    {
        var operationDocumentInfo = context.OperationDocumentInfo;

        if (operationDocumentInfo.Document is null)
        {
            throw new InvalidOperationException(
                "The operation document info is not available in the context.");
        }

        if (context.GetOperationPlan() is not null)
        {
            return next(context);
        }

        // Normalizing de-fragmentizes the operation and removes statically excluded
        // selections; this runs at most once per operation, since the normalizer caches
        // its result for reuse by later requests.
        PlanOperation(context, operationDocumentInfo, context.GetNormalizedOperation());

        return next(context);
    }

    private void PlanOperation(
        RequestContext context,
        OperationDocumentInfo operationDocumentInfo,
        OperationDefinitionNode operation)
    {
        var operationId = context.GetOperationId();
        var operationHash = context.OperationDocumentInfo.Hash.Value;
        var operationShortHash = operationHash[..8];

        using var scope = _diagnosticsEvents.PlanOperation(context, operationId);
        var inFlightPlan = context.Features.Get<TaskCompletionSource<OperationPlan>>();

        try
        {
            // After optimizing the query structure we can begin the planning process.
            var operationPlan =
                _planner.CreatePlan(
                    operationId,
                    operationHash,
                    operationShortHash,
                    operation,
                    context.RequestAborted);
            OnAfterPlanCompleted(operationDocumentInfo, operationPlan);

            // Setting the plan caches it and releases every coalesced follower right away,
            // before this (the leader's) request continues into execution, if this context
            // is the leader of an in-flight entry; see SetOperationPlan. A failure further
            // downstream then affects only the leader.
            context.SetOperationPlan(operationPlan);
        }
        catch (Exception ex)
        {
            _diagnosticsEvents.PlanOperationError(context, operationId, ex);

            if (ex is OperationCanceledException cancellationException)
            {
                inFlightPlan?.TrySetCanceled(cancellationException.CancellationToken);
            }
            else
            {
                inFlightPlan?.TrySetException(ex);
            }

            throw;
        }
    }

    private void OnAfterPlanCompleted(
        OperationDocumentInfo operationDocumentInfo,
        OperationPlan operationPlan)
    {
        switch (_interceptors.Length)
        {
            case 1:
                _interceptors[0].OnAfterPlanCompleted(operationDocumentInfo, operationPlan);
                break;

            case > 1:
                foreach (var interceptor in _interceptors)
                {
                    interceptor.OnAfterPlanCompleted(operationDocumentInfo, operationPlan);
                }

                break;
        }
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (fc, next) =>
            {
                var planner = fc.SchemaServices.GetRequiredService<OperationPlanner>();
                var interceptors = fc.SchemaServices.GetService<IEnumerable<IOperationPlannerInterceptor>>();
                var diagnosticEvents = fc.SchemaServices.GetRequiredService<IFusionExecutionDiagnosticEvents>();
                var middleware = new OperationPlanMiddleware(
                    planner,
                    interceptors,
                    diagnosticEvents);
                return requestContext => middleware.InvokeAsync(requestContext, next);
            },
            WellKnownRequestMiddleware.OperationPlanMiddleware);
}
