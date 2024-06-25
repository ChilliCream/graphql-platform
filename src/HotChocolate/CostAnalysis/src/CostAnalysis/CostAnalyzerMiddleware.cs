using HotChocolate.CostAnalysis.Caching;
using HotChocolate.CostAnalysis.Utilities;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

internal sealed class CostAnalyzerMiddleware(
    RequestDelegate next,
    CostOptions options,
    DocumentValidatorContextPool contextPool,
    ICostMetricsCache cache,
    IExecutionDiagnosticEvents diagnosticEvents)
{
    public async ValueTask InvokeAsync(IRequestContext context)
    {
        if (context.Document is null || OperationDocumentId.IsNullOrEmpty(context.DocumentId))
        {
            context.Result = ResultHelper.StateInvalidForCostAnalysis();
            return;
        }

        // we check if the operation id is already set and if not we create one.
        var operationId = context.OperationId;
        if (operationId is null)
        {
            operationId = context.CreateCacheId(context.DocumentId.Value.Value, context.Request.OperationName);
            context.OperationId = operationId;
        }

        var mode = context.GetCostAnalyzerMode(options);
        DocumentValidatorContext? validatorContext = null;
        CostMetrics? costMetrics;

        try
        {
            using var scope = diagnosticEvents.AnalyzeOperationCost(context);

            if (!cache.TryGetCostMetrics(operationId, out costMetrics))
            {
                // we check if the operation was already resolved by another middleware,
                // if not we resolve the operation.
                var document = context.Document;
                var operationDefinition =
                    context.Operation?.Definition ?? document.GetOperation(context.Request.OperationName);

                validatorContext = contextPool.Get();
                PrepareContext(context, document, validatorContext);

                var analyzer = new CostAnalyzer(options);
                costMetrics = analyzer.Analyze(operationDefinition, validatorContext);
                cache.TryAddCostMetrics(operationId, costMetrics);
            }

            context.ContextData.Add(WellKnownContextData.CostMetrics, costMetrics);
            diagnosticEvents.OperationCost(context, costMetrics.FieldCost, costMetrics.TypeCost);

            if (mode is CostAnalyzerMode.Enforce or CostAnalyzerMode.EnforceAndReport)
            {
                if (costMetrics.FieldCost > options.MaxFieldCost)
                {
                    context.Result = ErrorHelper.MaxFieldCostReached(
                        costMetrics,
                        options.MaxFieldCost,
                        mode == CostAnalyzerMode.EnforceAndReport);
                    return;
                }

                if (costMetrics.TypeCost > options.MaxTypeCost)
                {
                    context.Result = ErrorHelper.MaxTypeCostReached(
                        costMetrics,
                        options.MaxTypeCost,
                        mode == CostAnalyzerMode.EnforceAndReport);
                    return;
                }
            }
        }
        finally
        {
            if (validatorContext is not null)
            {
                validatorContext.Clear();
                contextPool.Return(validatorContext);
            }
        }

        switch (mode)
        {
            case CostAnalyzerMode.Analysis:
            case CostAnalyzerMode.Enforce:
                await next(context).ConfigureAwait(false);
                break;

            case CostAnalyzerMode.EnforceAndReport:
                await next(context).ConfigureAwait(false);
                context.Result = context.Result.AddCostMetrics(costMetrics);
                break;

            case CostAnalyzerMode.ValidateAndReport:
                context.Result = costMetrics.CreateResult();
                break;

            default:
                throw new NotSupportedException();
        }
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
            // this needs to be a schema service
            var options = core.SchemaServices.GetRequiredService<CostOptions>();
            var contextPool = core.Services.GetRequiredService<DocumentValidatorContextPool>();
            var cache = core.Services.GetRequiredService<ICostMetricsCache>();
            var diagnosticEvents = core.Services.GetRequiredService<IExecutionDiagnosticEvents>();

            var middleware = new CostAnalyzerMiddleware(
                next,
                options,
                contextPool,
                cache,
                diagnosticEvents);

            return context => middleware.InvokeAsync(context);
        };
    }
}
