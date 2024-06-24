using System.Collections.Immutable;
using HotChocolate.CostAnalysis.Caching;
using HotChocolate.Execution;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.CostAnalysis;

internal sealed class CostAnalysisMiddleware(
    RequestDelegate next,
    CostAnalysisOptions options,
    DocumentValidatorContextPool contextPool,
    ICostMetricsCache cache)
{
    public async ValueTask InvokeAsync(IRequestContext context)
    {
        if (context.Document is null || OperationDocumentId.IsNullOrEmpty(context.DocumentId))
        {
            context.Result = StateInvalidForCostAnalysis();
            return;
        }

        // we check if the operation id is already set and if not we create one.
        var operationId = context.OperationId;
        if (operationId is null)
        {
            operationId = context.CreateCacheId(context.DocumentId.Value.Value, context.Request.OperationName);
            context.OperationId = operationId;
        }

        DocumentValidatorContext? validatorContext = null;
        CostMetrics? costMetrics;

        try
        {
            if (!cache.TryGetCostMetrics(operationId, out costMetrics))
            {
                // we check if the operation was already resolved by another middleware,
                // if not we resolve the operation.
                var document = context.Document;
                var operationDefinition =
                    context.Operation?.Definition ??
                    document.GetOperation(context.Request.OperationName);

                validatorContext = contextPool.Get();
                PrepareContext(context, document, validatorContext);

                var analyzer = new CostAnalyzer();
                costMetrics = analyzer.Analyze(operationDefinition, validatorContext);
                cache.TryAddCostMetrics(operationId, costMetrics);
            }

            context.ContextData.Add(CostMetricsKey, costMetrics);

            if (costMetrics.FieldCost > options.MaxFieldCost)
            {
                context.Result = ErrorHelper.MaxFieldCostReached(
                    costMetrics.FieldCost,
                    options.MaxFieldCost);
                return;
            }

            if (costMetrics.TypeCost > options.MaxTypeCost)
            {
                context.Result = ErrorHelper.MaxTypeCostReached(
                    costMetrics.TypeCost,
                    options.MaxTypeCost);
                return;
            }
        }
        finally
        {
            if(validatorContext is not null)
            {
                validatorContext.Clear();
                contextPool.Return(validatorContext);
            }
        }

        await next(context).ConfigureAwait(false);

        if (context.ContextData.ContainsKey("cost"))
        {
            switch (context.Result)
            {
                case OperationResult result:
                    context.Result = AddCostMetrics(result, costMetrics);
                    return;

                case ResponseStream result:
                {
                    context.Result = AddCostMetrics(result, costMetrics);
                    return;
                }

                case OperationResultBatch result:
                {
                    var results = new IExecutionResult[result.Results.Count];
                    ImmutableDictionary<string, object?>? costMetricsMap = null;

                    for (var i = 0; i < result.Results.Count; i++)
                    {
                        switch (result.Results[i])
                        {
                            case OperationResult operationResult:
                                costMetricsMap ??= CreateCostMetricsMap(costMetrics);
                                results[i] = operationResult.WithExtensions(
                                    AddCostMetrics(operationResult.Extensions, costMetricsMap));
                                break;

                            case ResponseStream responseStream:
                                context.Result = AddCostMetrics(responseStream, costMetrics);
                                break;

                            default:
                                throw new NotSupportedException();
                        }
                    }

                    context.Result = new OperationResultBatch(results);
                    return;
                }
            }
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

    private static OperationResult AddCostMetrics(
        OperationResult operationResult,
        CostMetrics costMetrics)
    {
        var extensions = AddCostMetrics(operationResult.Extensions, costMetrics);
        return operationResult.WithExtensions(extensions);
    }

    private static ResponseStream AddCostMetrics(
        ResponseStream responseStream,
        CostMetrics costMetrics)
    {
        var onFirstResult = responseStream.OnFirstResult;

        if(onFirstResult.Count == 0)
        {
            onFirstResult =
                ImmutableArray.Create<Func<IOperationResult, IOperationResult>>(
                    result => result is OperationResult operationResult
                        ? operationResult.WithExtensions(AddCostMetrics(operationResult.Extensions, costMetrics))
                        : result);

            return responseStream.WithOnFirstResult(onFirstResult);
        }

        if(onFirstResult is ImmutableArray<Func<IOperationResult, IOperationResult>> immutable)
        {
            onFirstResult = immutable.Add(
                result => result is OperationResult operationResult
                    ? operationResult.WithExtensions(AddCostMetrics(operationResult.Extensions, costMetrics))
                    : result);

            return responseStream.WithOnFirstResult(onFirstResult);
        }

        var builder = ImmutableArray.CreateBuilder<Func<IOperationResult, IOperationResult>>();
        builder.AddRange(onFirstResult);
        builder.Add(
            result => result is OperationResult operationResult
                ? operationResult.WithExtensions(AddCostMetrics(operationResult.Extensions, costMetrics))
                : result);
        return responseStream.WithOnFirstResult(builder.ToImmutable());
    }

    private static IReadOnlyDictionary<string, object?> AddCostMetrics(
        IReadOnlyDictionary<string, object?>? extensions,
        CostMetrics costMetrics)
    {
        var costMetricsMap = CreateCostMetricsMap(costMetrics);
        return AddCostMetrics(extensions, costMetricsMap);
    }

    private static IReadOnlyDictionary<string, object?> AddCostMetrics(
        IReadOnlyDictionary<string, object?>? extensions,
        ImmutableDictionary<string, object?> costMetrics)
    {
        const string costKey = "cost";

        if(extensions is null || extensions.Count == 0)
        {
            return ImmutableDictionary<string, object?>.Empty.Add(costKey, costMetrics);
        }

        if(extensions is ImmutableDictionary<string, object?> immutable)
        {
            return immutable.Add(costKey, costMetrics);
        }

        var builder = ImmutableDictionary.CreateBuilder<string, object?>();
        builder.AddRange(extensions);
        builder.Add(costKey, costMetrics);
        return builder.ToImmutable();
    }

    private static ImmutableDictionary<string, object?> CreateCostMetricsMap(
        CostMetrics costMetrics)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, object?>();
        builder.Add("fieldCost", costMetrics.FieldCost);
        builder.Add("typeCost", costMetrics.TypeCost);
        return builder.ToImmutable();
    }

    public static IOperationResult StateInvalidForCostAnalysis() =>
        OperationResultBuilder.CreateError(
            ErrorBuilder.New()
                .SetMessage("The query request contains no document or no document id.")
                .SetCode(ErrorCodes.Execution.QueryNotFound)
                .Build());

    public static RequestCoreMiddleware Create()
    {
        return (core, next) =>
        {
            // this needs to be a schema service
            var options = core.SchemaServices.GetRequiredService<CostAnalysisOptions>();
            var contextPool = core.Services.GetRequiredService<DocumentValidatorContextPool>();
            var cache = core.Services.GetRequiredService<ICostMetricsCache>();

            var middleware = new CostAnalysisMiddleware(
                next,
                options,
                contextPool,
                cache);

            return context => middleware.InvokeAsync(context);
        };
    }
}
