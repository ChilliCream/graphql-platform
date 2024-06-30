using System.Collections.Immutable;
using HotChocolate.Execution;

namespace HotChocolate.CostAnalysis.Utilities;

internal static class ResultHelper
{
    private static readonly ImmutableDictionary<string, object?> _validationError
        = ImmutableDictionary<string, object?>.Empty
            .Add(WellKnownContextData.ValidationErrors, true);
    private static readonly ImmutableDictionary<string, object?> _ok
        = ImmutableDictionary<string, object?>.Empty
            .Add(WellKnownContextData.HttpStatusCode, 200);

    public static IExecutionResult CreateError(IError error, CostMetrics? costMetrics)
    {
        IReadOnlyDictionary<string, object?>? extensions = null;
        if (costMetrics is not null)
        {
            extensions = AddCostMetrics(extensions, costMetrics);
        }

        return error is AggregateError aggregateError
            ? CreateError(aggregateError.Errors, costMetrics)
            : new OperationResult(
                null,
                ImmutableArray.Create(error),
                extensions: extensions,
                contextData: _validationError);
    }

    public static IExecutionResult CreateError(IReadOnlyList<IError> errors, CostMetrics? costMetrics)
    {
        IReadOnlyDictionary<string, object?>? extensions = null;
        if (costMetrics is not null)
        {
            extensions = AddCostMetrics(extensions, costMetrics);
        }

        return new OperationResult(
            null,
            errors,
            extensions: extensions,
            contextData: _validationError);
    }

    public static IExecutionResult CreateResult(this CostMetrics costMetrics)
    {
        var extensions = AddCostMetrics(ImmutableDictionary<string, object?>.Empty, costMetrics);

        return new OperationResult(
            data: null,
            errors: null,
            extensions: extensions,
            contextData: _ok,
            items: null,
            incremental: null,
            label: null,
            path: null,
            hasNext: null,
            cleanupTasks: [],
            isDataSet: false,
            requestIndex: null,
            variableIndex: null,
            skipValidation: true);
    }

    public static IExecutionResult AddCostMetrics(
        this IExecutionResult? result,
        CostMetrics costMetrics)
    {
        switch (result)
        {
            case OperationResult r:
                return AddCostMetrics(r, costMetrics);

            case ResponseStream r:
            {
                return AddCostMetrics(r, costMetrics);
            }

            case OperationResultBatch r:
            {
                var results = new IExecutionResult[r.Results.Count];
                IImmutableDictionary<string, object?>? costMetricsMap = null;

                for (var i = 0; i < r.Results.Count; i++)
                {
                    switch (r.Results[i])
                    {
                        case OperationResult operationResult:
                            costMetricsMap ??= CreateCostMetricsMap(costMetrics);
                            results[i] = operationResult.WithExtensions(
                                AddCostMetrics(operationResult.Extensions, costMetricsMap));
                            break;

                        case ResponseStream responseStream:
                            return AddCostMetrics(responseStream, costMetrics);

                        default:
                            throw new NotSupportedException();
                    }
                }

                return new OperationResultBatch(results);
            }

            default:
                throw new NotSupportedException();
        }
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
        IImmutableDictionary<string, object?> costMetrics)
    {
        const string costKey = "operationCost";

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

    private static IImmutableDictionary<string, object?> CreateCostMetricsMap(
        CostMetrics costMetrics)
    {
        var builder = ImmutableSortedDictionary.CreateBuilder<string, object?>(StringComparer.Ordinal);
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
}
