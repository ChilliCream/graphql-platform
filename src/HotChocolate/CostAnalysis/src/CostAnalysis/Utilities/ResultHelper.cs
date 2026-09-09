using System.Collections.Immutable;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;

namespace HotChocolate.CostAnalysis.Utilities;

internal static class ResultHelper
{
    private static readonly ImmutableDictionary<string, object?> s_validationError
        = ImmutableDictionary<string, object?>.Empty
            .Add(ExecutionContextData.ValidationErrors, true);
    private static readonly ImmutableDictionary<string, object?> s_ok
        = ImmutableDictionary<string, object?>.Empty
            .Add(ExecutionContextData.HttpStatusCode, 200);

    public static IExecutionResult CreateError(IError error, CostMetrics? costMetrics)
    {
        ImmutableOrderedDictionary<string, object?>? extensions = null;
        if (costMetrics is not null)
        {
            extensions = AddCostMetrics(extensions, costMetrics);
        }

        return error is AggregateError aggregateError
            ? CreateError(aggregateError.Errors, costMetrics)
            : new OperationResult([error], extensions) { ContextData = s_validationError };
    }

    public static IExecutionResult CreateError(IReadOnlyList<IError> errors, CostMetrics? costMetrics)
    {
        ImmutableOrderedDictionary<string, object?>? extensions = null;
        if (costMetrics is not null)
        {
            extensions = AddCostMetrics(extensions, costMetrics);
        }

        return new OperationResult([.. errors], extensions) { ContextData = s_validationError };
    }

    public static IExecutionResult CreateResult(this CostMetrics costMetrics)
    {
        var extensions = AddCostMetrics([], costMetrics);
        return new OperationResult(extensions: extensions) { ContextData = s_ok };
    }

    public static IExecutionResult AddCostMetrics(
        this IExecutionResult? result,
        CostMetrics costMetrics)
    {
        switch (result)
        {
            case OperationResult operationResult:
                return AddCostMetrics(operationResult, costMetrics);

            case ResponseStream responseStream:
                return AddCostMetrics(responseStream, costMetrics);

            case OperationResultBatch batch:
                foreach (var current in batch.Results)
                {
                    switch (current)
                    {
                        case OperationResult operationResult:
                            AddCostMetrics(operationResult, costMetrics);
                            break;

                        case ResponseStream responseStream:
                            AddCostMetrics(responseStream, costMetrics);
                            break;

                        default:
                            return ErrorHelper.StateInvalidForCostAnalysis();
                    }
                }

                return batch;

            default:
                return ErrorHelper.StateInvalidForCostAnalysis();
        }
    }

    public static IExecutionResult AddCostMetrics(
        this IExecutionResult? result,
        ImmutableArray<CostMetrics> costMetrics)
    {
        if (costMetrics.IsDefaultOrEmpty)
        {
            return ErrorHelper.StateInvalidForCostAnalysis();
        }

        switch (result)
        {
            case OperationResult r:
                return AddCostMetrics(r, costMetrics[0]);

            case ResponseStream r:
                return AddCostMetrics(r, costMetrics[0]);

            case OperationResultBatch r:
                if (r.Results.Count != costMetrics.Length)
                {
                    return ErrorHelper.StateInvalidForCostAnalysis();
                }

                for (var i = 0; i < r.Results.Count; i++)
                {
                    switch (r.Results[i])
                    {
                        case OperationResult operationResult:
                            AddCostMetrics(operationResult, costMetrics[i]);
                            break;

                        case ResponseStream responseStream:
                            AddCostMetrics(responseStream, costMetrics[i]);
                            break;

                        default:
                            return ErrorHelper.StateInvalidForCostAnalysis();
                    }
                }

                return r;

            default:
                return ErrorHelper.StateInvalidForCostAnalysis();
        }
    }

    private static OperationResult AddCostMetrics(
        OperationResult operationResult,
        CostMetrics costMetrics)
    {
        var extensions = AddCostMetrics(operationResult.Extensions, costMetrics);
        operationResult.Extensions = extensions;
        return operationResult;
    }

    private static ResponseStream AddCostMetrics(
        ResponseStream responseStream,
        CostMetrics costMetrics)
    {
        var onFirstResult = responseStream.OnFirstResult;

        if (onFirstResult.IsEmpty)
        {
            onFirstResult =
                [
                    result =>
                    {
                        result.Extensions = AddCostMetrics(result.Extensions, costMetrics);
                        return result;
                    }
                ];
        }
        else
        {
            onFirstResult =
                onFirstResult.Add(
                    result =>
                    {
                        result.Extensions = AddCostMetrics(result.Extensions, costMetrics);
                        return result;
                    });
        }

        responseStream.OnFirstResult = onFirstResult;
        return responseStream;
    }

    private static ImmutableOrderedDictionary<string, object?> AddCostMetrics(
        ImmutableOrderedDictionary<string, object?>? extensions,
        CostMetrics costMetrics)
    {
        var costMetricsMap = CreateCostMetricsMap(costMetrics);
        return AddCostMetrics(extensions, costMetricsMap);
    }

    private static ImmutableOrderedDictionary<string, object?> AddCostMetrics(
        ImmutableOrderedDictionary<string, object?>? extensions,
        ImmutableOrderedDictionary<string, object?> costMetrics)
    {
        const string costKey = "operationCost";

        if (extensions is null || extensions.Count == 0)
        {
            return ImmutableOrderedDictionary<string, object?>.Empty.Add(costKey, costMetrics);
        }

        return extensions.Add(costKey, costMetrics);
    }

    private static ImmutableOrderedDictionary<string, object?> CreateCostMetricsMap(
        CostMetrics costMetrics)
    {
        var builder = ImmutableOrderedDictionary.CreateBuilder<string, object?>();
        builder.Add("fieldCost", FormatValue(costMetrics.FieldCost));
        builder.Add("typeCost", FormatValue(costMetrics.TypeCost));

        if (costMetrics.MaxResponseSize is { } maxResponseSize)
        {
            builder.Add("maxResponseSize", FormatValue(maxResponseSize));
        }

        return builder.ToImmutable();
    }

    internal static object FormatValue(double value)
        => double.IsPositiveInfinity(value) ? "Infinity" : value;
}
