using HotChocolate.Execution;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

public sealed class OperationPlanMiddleware
{
    private readonly OperationPlanner _planner;

    public OperationPlanMiddleware(OperationPlanner planner)
    {
        ArgumentNullException.ThrowIfNull(planner);

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

        if (context.GetOperationExecutionPlan() is not null)
        {
            return next(context);
        }

        // Before we can plan an operation, we must defragmentize it.
        // Defragemntization is a process that removes fragment spreads
        // and fragment definitions and inlines them into the operation definition.
        var rewriter = new InlineFragmentOperationRewriter(context.Schema);
        var rewritten = rewriter.RewriteDocument(operationDocumentInfo.Document, context.Request.OperationName);
        var operation = GetOperation(rewritten);
        var executionPlan = _planner.CreatePlan(operation);
        context.SetOperationExecutionPlan(executionPlan);

        return next(context);

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
            var middleware = new OperationPlanMiddleware(planner);
            return requestContext => middleware.InvokeAsync(requestContext, next);
        };
    }
}
