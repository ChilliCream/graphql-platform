using HotChocolate.CostAnalysis.Caching;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.CostAnalysis.WellKnownContextData;

namespace HotChocolate.CostAnalysis;

internal sealed class CostAnalysisMiddleware(
    RequestDelegate next,
    CostAnalysisOptions options,
    DocumentValidatorContextPool contextPool,
    ICostMetricsCache cache,
    CostAnalysisVisitor costAnalysisVisitor)
{
    public async ValueTask InvokeAsync(IRequestContext context)
    {
        if (context.Document is not null && context.OperationId is not null)
        {
            var document = context.Document;
            var operationDefinition =
                context.Operation?.Definition ??
                document.GetOperation(context.Request.OperationName);

            var validatorContext = contextPool.Get();

            try
            {
                if (!cache.TryGetCostMetrics(context.OperationId, out var costMetrics))
                {
                    PrepareContext(context, document, validatorContext);

                    costAnalysisVisitor.Visit(operationDefinition, validatorContext);

                    costMetrics = (CostMetrics)validatorContext.ContextData[RequestCosts]!;

                    cache.TryAddCostMetrics(context.OperationId, costMetrics);
                }

                if (costMetrics.FieldCost > options.MaxFieldCost)
                {
                    // FIXME: This is not ending the request.
                    context.Result = ErrorHelper.MaxFieldCostReached(
                        costMetrics.FieldCost,
                        options.MaxFieldCost);

                    return;
                }

                if (costMetrics.TypeCost > options.MaxTypeCost)
                {
                    // FIXME: This is not ending the request.
                    context.Result = ErrorHelper.MaxTypeCostReached(
                        costMetrics.TypeCost,
                        options.MaxTypeCost);

                    return;
                }
            }
            finally
            {
                validatorContext.Clear();
                contextPool.Return(validatorContext);
            }
        }

        await next(context).ConfigureAwait(false);
    }

    private static void PrepareContext(
        IRequestContext requestContext,
        DocumentNode document,
        DocumentValidatorContext validatorContext)
    {
        validatorContext.Schema = requestContext.Schema;

        foreach (var definitionNode in document.Definitions)
        {
            if (definitionNode is FragmentDefinitionNode fragmentDefinition)
            {
                validatorContext.Fragments[fragmentDefinition.Name.Value] = fragmentDefinition;
            }
        }

        validatorContext.ContextData = requestContext.ContextData;
    }

    public static RequestCoreMiddleware Create()
    {
        return (core, next) =>
        {
            var options = core.Services.GetRequiredService<CostAnalysisOptions>();
            var contextPool = core.Services.GetRequiredService<DocumentValidatorContextPool>();
            var cache = core.Services.GetRequiredService<ICostMetricsCache>();

            var middleware = new CostAnalysisMiddleware(
                next,
                options,
                contextPool,
                cache,
                new CostAnalysisVisitor());

            return context => middleware.InvokeAsync(context);
        };
    }
}
