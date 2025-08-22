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
    private readonly InlineFragmentOperationRewriter _rewriter;
    private readonly IFusionExecutionDiagnosticEvents _diagnosticsEvents;

    private OperationPlanMiddleware(
        ISchemaDefinition schema,
        OperationPlanner planner,
        IFusionExecutionDiagnosticEvents diagnosticsEvents)
    {
        _rewriter = new InlineFragmentOperationRewriter(schema);
        _planner = planner;
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

        PlanOperation(context, operationDocumentInfo.Document);

        return next(context);
    }

    private void PlanOperation(RequestContext context, DocumentNode operationDocument)
    {
        using var scope = _diagnosticsEvents.PlanOperation(context);
        var operationId = context.GetOperationId();
        var operationHash = context.OperationDocumentInfo.Hash.Value;
        var operationShortHash = operationHash[..8];

        // Before we can plan an operation, we must defragmentize it and remove statical include conditions.
        var rewritten = _rewriter.RewriteDocument(operationDocument, context.Request.OperationName);
        var operation = rewritten.GetOperation(context.Request.OperationName);

        // After optimizing the query structure we can begin the planning process.
        var executionPlan = _planner.CreatePlan(operationId, operationHash, operationShortHash, operation);
        context.SetOperationPlan(executionPlan);
    }

    public static RequestMiddleware Create()
    {
        return static (fc, next) =>
        {
            var planner = fc.SchemaServices.GetRequiredService<OperationPlanner>();
            var diagnosticEvents = fc.SchemaServices.GetRequiredService<IFusionExecutionDiagnosticEvents>();
            var middleware = new OperationPlanMiddleware(fc.Schema, planner, diagnosticEvents);
            return requestContext => middleware.InvokeAsync(requestContext, next);
        };
    }
}
