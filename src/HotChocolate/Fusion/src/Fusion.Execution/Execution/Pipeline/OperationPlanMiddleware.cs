using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning;
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

        PlanOperation(context, operationDocumentInfo);

        return next(context);
    }

    private void PlanOperation(
        RequestContext context,
        OperationDocumentInfo operationDocumentInfo)
    {
        var operationId = context.GetOperationId();
        var operationHash = context.OperationDocumentInfo.Hash.Value;
        var operationShortHash = operationHash[..8];

        using var scope = _diagnosticsEvents.PlanOperation(context, operationId);

        try
        {
            // The document has already been de-fragmentized and had its statically excluded
            // selections removed by the DocumentNormalizationMiddleware.
            if (!context.TryGetNormalizedOperation(out var operation))
            {
                throw new InvalidOperationException(
                    "The normalized operation is not available in the context.");
            }

            // After optimizing the query structure we can begin the planning process.
            var operationPlan =
                _planner.CreatePlan(
                    operationId,
                    operationHash,
                    operationShortHash,
                    operation,
                    context.RequestAborted);
            OnAfterPlanCompleted(operationDocumentInfo, operationPlan);
            context.SetOperationPlan(operationPlan);
        }
        catch (Exception ex)
        {
            _diagnosticsEvents.PlanOperationError(context, operationId, ex);

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
