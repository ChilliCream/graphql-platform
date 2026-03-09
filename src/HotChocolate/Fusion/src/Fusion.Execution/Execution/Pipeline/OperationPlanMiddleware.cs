using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationPlanMiddleware
{
    private readonly OperationPlanner _planner;
    private readonly DocumentRewriter _documentRewriter;
    private readonly IOperationPlannerInterceptor[] _interceptors;
    private readonly IFusionExecutionDiagnosticEvents _diagnosticsEvents;

    private OperationPlanMiddleware(
        ISchemaDefinition schema,
        OperationPlanner planner,
        IEnumerable<IOperationPlannerInterceptor>? interceptors,
        IFusionExecutionDiagnosticEvents diagnosticsEvents)
    {
        _documentRewriter = new DocumentRewriter(schema, removeStaticallyExcludedSelections: true);
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

        PlanOperation(context, operationDocumentInfo, operationDocumentInfo.Document);

        return next(context);
    }

    private void PlanOperation(
        RequestContext context,
        OperationDocumentInfo operationDocumentInfo,
        DocumentNode operationDocument)
    {
        var operationId = context.GetOperationId();
        var operationHash = context.OperationDocumentInfo.Hash.Value;
        var operationShortHash = operationHash[..8];

        using var scope = _diagnosticsEvents.PlanOperation(context, operationId);
        var inFlightPlan = context.Features.Get<TaskCompletionSource<OperationPlan>>();

        try
        {
            // Before we can plan an operation, we must de-fragmentize it and remove static include conditions.
            var rewritten = _documentRewriter.RewriteDocument(operationDocument, context.Request.OperationName);
            var operation = rewritten.GetOperation(context.Request.OperationName);

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
            inFlightPlan?.TrySetResult(operationPlan);
        }
        catch (Exception ex)
        {
            inFlightPlan?.TrySetException(ex);
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
                var middleware = new OperationPlanMiddleware(fc.Schema, planner, interceptors, diagnosticEvents);
                return requestContext => middleware.InvokeAsync(requestContext, next);
            },
            WellKnownRequestMiddleware.OperationPlanMiddleware);
}
