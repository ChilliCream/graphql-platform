using System.Diagnostics.CodeAnalysis;
using HotChocolate.CostAnalysis.Caching;
using HotChocolate.CostAnalysis.Utilities;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using ErrorHelper = HotChocolate.CostAnalysis.Utilities.ErrorHelper;

namespace HotChocolate.CostAnalysis;

internal sealed class CostAnalyzerMiddleware(
    RequestDelegate next,
    [SchemaService] RequestCostOptions options,
    ObjectPool<DocumentValidatorContext> contextPool,
    ICostMetricsCache cache,
    [SchemaService] IExecutionDiagnosticEvents diagnosticEvents)
{
    public async ValueTask InvokeAsync(RequestContext context)
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
            operationId = context.CreateCacheId();
            context.OperationId = operationId;
        }

        var requestOptions = context.TryGetCostOptions() ?? options;
        var mode = context.GetCostAnalyzerMode(requestOptions);

        if (mode == CostAnalyzerMode.Skip)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (!TryAnalyze(context, requestOptions, mode, context.Document, operationId, out var costMetrics))
        {
            // a error happened during the analysis and the error is already set.
            return;
        }

        if ((mode & CostAnalyzerMode.Execute) == CostAnalyzerMode.Execute)
        {
            await next(context).ConfigureAwait(false);
        }

        if ((mode & CostAnalyzerMode.Report) == CostAnalyzerMode.Report)
        {
            context.Result =
                context.Result is null
                    ? costMetrics.CreateResult()
                    : context.Result.AddCostMetrics(costMetrics);
        }
    }

    private bool TryAnalyze(
        RequestContext context,
        RequestCostOptions requestOptions,
        CostAnalyzerMode mode,
        DocumentNode document,
        string operationId,
        [NotNullWhen(true)] out CostMetrics? costMetrics)
    {
        using var scope = diagnosticEvents.AnalyzeOperationCost(context);
        DocumentValidatorContext? validatorContext = null;

        try
        {
            if (!cache.TryGetCostMetrics(operationId, out costMetrics))
            {
                // we check if the operation was already resolved by another middleware,
                // if not we resolve the operation.
                var operationDefinition =
                    context.Operation?.Definition
                        ?? document.GetOperation(context.Request.OperationName);

                validatorContext = contextPool.Get();
                validatorContext.Initialize(
                    context.Schema,
                    context.DocumentId!.Value,
                    document,
                    maxAllowedErrors: 1,
                    context.Features);

                var analyzer = new CostAnalyzer(requestOptions);
                costMetrics = analyzer.Analyze(operationDefinition, validatorContext);
                cache.TryAddCostMetrics(operationId, costMetrics);
            }

            context.SetCostMetrics(costMetrics);
            diagnosticEvents.OperationCost(context, costMetrics.FieldCost, costMetrics.TypeCost);

            if ((mode & CostAnalyzerMode.Enforce) == CostAnalyzerMode.Enforce)
            {
                if (costMetrics.FieldCost > requestOptions.MaxFieldCost)
                {
                    context.Result = ErrorHelper.MaxFieldCostReached(
                        costMetrics,
                        requestOptions.MaxFieldCost,
                        (mode & CostAnalyzerMode.Report) == CostAnalyzerMode.Report);
                    return false;
                }

                if (costMetrics.TypeCost > requestOptions.MaxTypeCost)
                {
                    context.Result = ErrorHelper.MaxTypeCostReached(
                        costMetrics,
                        requestOptions.MaxTypeCost,
                        (mode & CostAnalyzerMode.Report) == CostAnalyzerMode.Report);
                    return false;
                }
            }

            return true;
        }
        catch (GraphQLException ex)
        {
            context.Result = ResultHelper.CreateError(ex.Errors, null);
            costMetrics = null;
            return false;
        }
        finally
        {
            if (validatorContext is not null)
            {
                validatorContext.Clear();
                contextPool.Return(validatorContext);
            }
        }
    }

    public static RequestCoreMiddleware Create()
    {
        return (core, next) =>
        {
            // this needs to be a schema service
            var options = core.SchemaServices.GetRequiredService<RequestCostOptions>();
            var contextPool = core.Services.GetRequiredService<ObjectPool<DocumentValidatorContext>>();
            var cache = core.Services.GetRequiredService<ICostMetricsCache>();
            var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();

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
