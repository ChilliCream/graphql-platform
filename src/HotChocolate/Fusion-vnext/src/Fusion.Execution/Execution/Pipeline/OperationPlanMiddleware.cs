using HotChocolate.Execution;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationPlanMiddleware
{
    private readonly OperationPlanner _planner;
    private readonly InlineFragmentOperationRewriter _rewriter;

    private OperationPlanMiddleware(ISchemaDefinition schema, OperationPlanner planner)
    {
        ArgumentNullException.ThrowIfNull(planner);

        _rewriter = new InlineFragmentOperationRewriter(schema);
        _planner = planner;
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

        // Before we can plan an operation, we must defragmentize it and remove statical include conditions.
        var operationId = context.GetOperationId();
        var operationHash = context.OperationDocumentInfo.Hash.Value;
        var operationShortHash = operationHash[..8];
        var rewritten = _rewriter.RewriteDocument(operationDocumentInfo.Document, context.Request.OperationName);
        var operation = GetOperation(rewritten);
        var executionPlan = _planner.CreatePlan(operationId, operationHash, operationShortHash, operation);
        context.SetOperationPlan(executionPlan);

        return next(context);

        // TODO: this algorithm is wrong and will fail with multiple operations.
        static OperationDefinitionNode GetOperation(DocumentNode document)
        {
            for (var i = 0; i < document.Definitions.Count; i++)
            {
                if (document.Definitions[i] is OperationDefinitionNode operation)
                {
                    return operation;
                }
            }

            throw new InvalidOperationException(
                "The operation document does not contain an operation definition.");
        }
    }

    public static RequestMiddleware Create()
    {
        return static (fc, next) =>
        {
            var planner = fc.SchemaServices.GetRequiredService<OperationPlanner>();
            var middleware = new OperationPlanMiddleware(fc.Schema, planner);
            return requestContext => middleware.InvokeAsync(requestContext, next);
        };
    }
}
