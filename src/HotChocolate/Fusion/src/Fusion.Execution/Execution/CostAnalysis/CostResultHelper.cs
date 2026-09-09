using System.Collections.Immutable;
using HotChocolate.Collections.Immutable;
using HotChocolate.CostAnalysis;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.CostAnalysis;

internal static class CostResultHelper
{
    private static readonly ImmutableDictionary<string, object?> s_ok =
        ImmutableDictionary<string, object?>.Empty.Add(ExecutionContextData.HttpStatusCode, 200);

    public static IExecutionResult CreateResult(ImmutableArray<CostEstimate> estimates)
    {
        if (estimates.IsDefaultOrEmpty)
        {
            return ErrorHelper.StateInvalidForCostAnalysis();
        }

        if (estimates.Length == 1)
        {
            return CreateResult(estimates[0]);
        }

        var results = ImmutableList.CreateBuilder<IExecutionResult>();

        foreach (var estimate in estimates)
        {
            results.Add(CreateResult(estimate));
        }

        return new OperationResultBatch(results.ToImmutable());
    }

    public static IExecutionResult AddCost(
        IExecutionResult result,
        ImmutableArray<CostEstimate> estimates)
    {
        if (estimates.IsDefaultOrEmpty)
        {
            return ErrorHelper.StateInvalidForCostAnalysis();
        }

        return result switch
        {
            OperationResult operationResult
                => AddCost(operationResult, estimates[0]),
            ResponseStream responseStream
                => AddCost(responseStream, estimates[0]),
            OperationResultBatch batch
                => AddCost(batch, estimates),
            _ => ErrorHelper.StateInvalidForCostAnalysis()
        };
    }

    public static OperationResultBatch CreateErrorBatch(
        ImmutableArray<CostEstimate> estimates,
        CostLimitKind limitKind,
        double limit,
        bool report)
    {
        var results = ImmutableList.CreateBuilder<IExecutionResult>();

        foreach (var estimate in estimates)
        {
            results.Add(CreateError(estimate, limitKind, limit, report));
        }

        return new OperationResultBatch(results.ToImmutable());
    }

    public static OperationResult CreateError(
        CostEstimate estimate,
        CostLimitKind limitKind,
        double limit,
        bool report)
    {
        var result = limitKind switch
        {
            CostLimitKind.FieldCost => ErrorHelper.MaxFieldCostReached(estimate, limit),
            CostLimitKind.TypeCost => ErrorHelper.MaxTypeCostReached(estimate, limit),
            CostLimitKind.ResponseSize => ErrorHelper.MaxResponseSizeReached(estimate, limit),
            _ => ErrorHelper.StateInvalidForCostAnalysis()
        };

        return report ? AddCost(result, estimate) : result;
    }

    private static OperationResult CreateResult(CostEstimate estimate)
    {
        var result = new OperationResult(CreateExtensions(estimate));
        result.ContextData = s_ok;
        return result;
    }

    private static IExecutionResult AddCost(
        OperationResultBatch batch,
        ImmutableArray<CostEstimate> estimates)
    {
        if (batch.Results.Count != estimates.Length)
        {
            return ErrorHelper.StateInvalidForCostAnalysis();
        }

        for (var i = 0; i < estimates.Length; i++)
        {
            switch (batch.Results[i])
            {
                case OperationResult operationResult:
                    AddCost(operationResult, estimates[i]);
                    break;

                case ResponseStream responseStream:
                    AddCost(responseStream, estimates[i]);
                    break;

                default:
                    return ErrorHelper.StateInvalidForCostAnalysis();
            }
        }

        return batch;
    }

    private static OperationResult AddCost(OperationResult result, CostEstimate estimate)
    {
        result.Extensions = AddCost(result.Extensions, estimate);
        return result;
    }

    private static ResponseStream AddCost(ResponseStream stream, CostEstimate estimate)
    {
        stream.OnFirstResult = stream.OnFirstResult.Add(
            result =>
            {
                result.Extensions = AddCost(result.Extensions, estimate);
                return result;
            });
        return stream;
    }

    private static ImmutableOrderedDictionary<string, object?> AddCost(
        ImmutableOrderedDictionary<string, object?> extensions,
        CostEstimate estimate)
        => extensions.Add("operationCost", CreateCost(estimate));

    private static ImmutableOrderedDictionary<string, object?> CreateExtensions(CostEstimate estimate)
        => ImmutableOrderedDictionary<string, object?>.Empty.Add("operationCost", CreateCost(estimate));

    private static ImmutableOrderedDictionary<string, object?> CreateCost(CostEstimate estimate)
    {
        var builder = ImmutableOrderedDictionary.CreateBuilder<string, object?>();
        builder.Add("fieldCost", FormatValue(estimate.FieldCost));
        builder.Add("typeCost", FormatValue(estimate.TypeCost));

        if (estimate.MaxResponseSize is { } maxResponseSize)
        {
            builder.Add("maxResponseSize", FormatValue(maxResponseSize));
        }

        return builder.ToImmutable();
    }

    internal static object FormatValue(double value)
        => double.IsPositiveInfinity(value) ? "Infinity" : value;
}
