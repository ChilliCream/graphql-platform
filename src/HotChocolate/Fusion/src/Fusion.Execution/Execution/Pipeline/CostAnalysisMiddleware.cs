using System.Collections.Immutable;
using HotChocolate.Caching.Memory;
using HotChocolate.CostAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.CostAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class CostAnalysisMiddleware
{
    private readonly CostSchemaSnapshot _snapshot;
    private readonly Cache<CostPlan> _cache;
    private readonly FusionCostOptions _options;

    private CostAnalysisMiddleware(
        CostSchemaSnapshot snapshot,
        Cache<CostPlan> cache,
        FusionCostOptions options)
    {
        _snapshot = snapshot;
        _cache = cache;
        _options = options;
    }

    public ValueTask InvokeAsync(RequestContext context, RequestDelegate next)
    {
        var mode = GetMode(context);

        if (mode == CostAnalysisMode.Skip)
        {
            return next(context);
        }

        if (!context.TryGetNormalizedOperation(out var operation))
        {
            context.Result = ErrorHelper.StateInvalidForCostAnalysis();
            return default;
        }

        var operationId = context.GetOperationId();

        if (!_cache.TryGet(operationId, out var plan))
        {
            var analyses = CostAnalyses.Cost;

            if (_options.MaxResponseSize.HasValue)
            {
                analyses |= CostAnalyses.ResponseSize;
            }

            plan = CostPlanCompiler.Compile(
                _snapshot,
                context.GetNormalizedDocument(),
                operation,
                analyses);
            _cache.TryAdd(operationId, plan);
        }

        var isStaticBound = context.IsWarmupRequest() || context.VariableValues.IsDefaultOrEmpty;
        ImmutableArray<CostEstimate> estimates;

        if (isStaticBound)
        {
            estimates = [plan.EvaluateStaticBound()];
        }
        else
        {
            var builder = ImmutableArray.CreateBuilder<CostEstimate>(context.VariableValues.Length);
            var adapter = new CostVariableValuesAdapter();

            foreach (var variableValues in context.VariableValues)
            {
                adapter.SetValues(variableValues);
                builder.Add(plan.Evaluate(adapter));
            }

            estimates = builder.MoveToImmutable();
        }

        var analysisResult = new CostAnalysisResult(plan, estimates, isStaticBound);
        context.Features.Set(analysisResult);

        if ((mode & CostAnalysisMode.Enforce) == CostAnalysisMode.Enforce)
        {
            foreach (var estimate in estimates)
            {
                if (estimate.FieldCost > _options.MaxFieldCost)
                {
                    context.Result = ErrorHelper.MaxFieldCostReached(estimate, _options.MaxFieldCost);
                    return default;
                }

                if (estimate.TypeCost > _options.MaxTypeCost)
                {
                    context.Result = ErrorHelper.MaxTypeCostReached(estimate, _options.MaxTypeCost);
                    return default;
                }

                if (_options.MaxResponseSize is { } maxResponseSize
                    && estimate.MaxResponseSize > maxResponseSize)
                {
                    context.Result = ErrorHelper.MaxResponseSizeReached(estimate, maxResponseSize);
                    return default;
                }
            }
        }

        return (mode & CostAnalysisMode.Execute) == CostAnalysisMode.Execute
            ? next(context)
            : default;
    }

    private CostAnalysisMode GetMode(RequestContext context)
    {
        if (_options.SkipAnalyzer)
        {
            return CostAnalysisMode.Skip;
        }

        if (context.ContextData.ContainsKey(ExecutionContextData.ValidateCost))
        {
            return CostAnalysisMode.Analyze | CostAnalysisMode.Report;
        }

        var mode = CostAnalysisMode.Analyze | CostAnalysisMode.Execute;

        if (_options.EnforceCostLimits)
        {
            mode |= CostAnalysisMode.Enforce;
        }

        if (context.ContextData.ContainsKey(ExecutionContextData.ReportCost))
        {
            mode |= CostAnalysisMode.Report;
        }

        return mode;
    }

    public static RequestMiddlewareConfiguration Create()
        => new(
            (fc, next) =>
            {
                var snapshot = fc.SchemaServices.GetRequiredService<CostSchemaSnapshot>();
                var cache = fc.SchemaServices.GetRequiredService<Cache<CostPlan>>();
                var options = fc.SchemaServices.GetRequiredService<FusionRequestOptions>().Cost;
                var middleware = new CostAnalysisMiddleware(snapshot, cache, options);
                return context => middleware.InvokeAsync(context, next);
            },
            WellKnownRequestMiddleware.CostAnalyzerMiddleware);
}
