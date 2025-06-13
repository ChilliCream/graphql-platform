using HotChocolate.Execution;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

public sealed class ExecutionPlanMiddleware
{
    private readonly OperationPlanner _planner;

    public ExecutionPlanMiddleware(OperationPlanner planner)
    {
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
    }

    public ValueTask InvokeAsync(RequestContext context, RequestDelegate next)
    {
        var operationDocumentInfo = context.OperationDocumentInfo;

        if (operationDocumentInfo.Document is null)
        {
            throw new InvalidOperationException(
                "The operation document info is not available in the context.");
        }

        if (context.GetExecutionPlan() is not null)
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
        context.SetExecutionPlan(executionPlan);

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
        return static (factoryContext, next) =>
        {
            var planner = factoryContext.Services.GetRequiredService<OperationPlanner>();
            var middleware = new ExecutionPlanMiddleware(planner);
            return requestContext => middleware.InvokeAsync(requestContext, next);
        };
    }
}
