using System.Collections.Immutable;
using HotChocolate.Caching.Memory;
using HotChocolate.CostAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.CostAnalysis;
using HotChocolate.Fusion.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class CostAnalysisMiddleware
{
    private readonly CostSchemaIndex _schemaIndex;
    private readonly Cache<CostPlan> _cache;
    private readonly FusionCostOptions _options;
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;

    private CostAnalysisMiddleware(
        CostSchemaIndex schemaIndex,
        Cache<CostPlan> cache,
        FusionCostOptions options,
        IFusionExecutionDiagnosticEvents diagnosticEvents)
    {
        _schemaIndex = schemaIndex;
        _cache = cache;
        _options = options;
        _diagnosticEvents = diagnosticEvents;
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

        var isWarmup = context.IsWarmupRequest();

        // Coercion always precedes cost analysis and produces at least one variable set for a
        // non-warmup request, except when a variable batch request's payload is an explicitly
        // empty array. That state is invalid for cost analysis; the assumed bound is reserved for
        // warmup requests and must not leak back into the request path.
        if (!isWarmup && context.VariableValues.IsDefaultOrEmpty)
        {
            context.Result = ErrorHelper.StateInvalidForCostAnalysisMissingVariableValues();
            return default;
        }

        ImmutableArray<CostEstimate> estimates;
        CostEstimate? rejectedEstimate = null;
        CostLimitKind rejectedLimitKind = default;
        double rejectedLimit = default;

        using (_diagnosticEvents.AnalyzeOperationCost(context))
        {
            var operationId = context.GetOperationId();

            if (!_cache.TryGet(operationId, out var plan))
            {
                var analyses = CostAnalyses.Cost;

                if (_options.MaxResponseSize.HasValue)
                {
                    analyses |= CostAnalyses.ResponseSize;
                }

                plan = CostPlanCompiler.Compile(
                    _schemaIndex,
                    context.GetNormalizedDocument(),
                    operation,
                    analyses);
                _cache.TryAdd(operationId, plan);
            }

            var isAssumedBound = isWarmup;

            if (isAssumedBound)
            {
                var estimate = plan.EvaluateAssumedBound();
                estimates = [estimate];
                _diagnosticEvents.OperationCost(context, estimate.FieldCost, estimate.TypeCost);
            }
            else
            {
                var builder = ImmutableArray.CreateBuilder<CostEstimate>(context.VariableValues.Length);
                var adapter = new CostVariableValuesAdapter();

                foreach (var variableValues in context.VariableValues)
                {
                    adapter.SetValues(variableValues);
                    var estimate = plan.Evaluate(adapter);
                    builder.Add(estimate);
                    _diagnosticEvents.OperationCost(context, estimate.FieldCost, estimate.TypeCost);
                }

                estimates = builder.MoveToImmutable();
            }

            var analysisResult = new CostAnalysisResult(plan, estimates, isAssumedBound);
            context.Features.Set(analysisResult);

            if ((mode & CostAnalysisMode.Enforce) == CostAnalysisMode.Enforce)
            {
                if (estimates.Length == 1)
                {
                    TryGetViolation(
                        estimates[0],
                        out rejectedEstimate,
                        out rejectedLimitKind,
                        out rejectedLimit);
                }
                else
                {
                    TryGetBatchViolation(
                        estimates,
                        out rejectedEstimate,
                        out rejectedLimitKind,
                        out rejectedLimit);
                }
            }
        }

        if (rejectedEstimate is { } rejected)
        {
            var report = (mode & CostAnalysisMode.Report) == CostAnalysisMode.Report;
            context.Result = CostResultHelper.CreateError(
                rejected,
                rejectedLimitKind,
                rejectedLimit,
                report);

            return default;
        }

        if ((mode & CostAnalysisMode.Execute) == CostAnalysisMode.Execute)
        {
            var execution = next(context);

            if ((mode & CostAnalysisMode.Report) == CostAnalysisMode.Report)
            {
                return AwaitAndReportAsync(context, execution, estimates);
            }

            return execution;
        }

        if ((mode & CostAnalysisMode.Report) == CostAnalysisMode.Report)
        {
            context.Result = context.Result is null
                ? CostResultHelper.CreateResult(estimates)
                : CostResultHelper.AddCost(context.Result, estimates);
        }

        return default;
    }

    private bool TryGetViolation(
        CostEstimate estimate,
        out CostEstimate? rejectedEstimate,
        out CostLimitKind limitKind,
        out double limit)
    {
        if (estimate.FieldCost > _options.MaxFieldCost)
        {
            rejectedEstimate = estimate;
            limitKind = CostLimitKind.FieldCost;
            limit = _options.MaxFieldCost;
            return true;
        }

        if (estimate.TypeCost > _options.MaxTypeCost)
        {
            rejectedEstimate = estimate;
            limitKind = CostLimitKind.TypeCost;
            limit = _options.MaxTypeCost;
            return true;
        }

        if (_options.MaxResponseSize is { } maxResponseSize
            && estimate.MaxResponseSize > maxResponseSize)
        {
            rejectedEstimate = estimate;
            limitKind = CostLimitKind.ResponseSize;
            limit = maxResponseSize;
            return true;
        }

        rejectedEstimate = null;
        limitKind = default;
        limit = default;
        return false;
    }

    private bool TryGetBatchViolation(
        ImmutableArray<CostEstimate> estimates,
        out CostEstimate? rejectedEstimate,
        out CostLimitKind limitKind,
        out double limit)
    {
        var summedFieldCost = 0d;
        var summedTypeCost = 0d;

        foreach (var estimate in estimates)
        {
            summedFieldCost += estimate.FieldCost;
            summedTypeCost += estimate.TypeCost;
        }

        if (summedFieldCost > _options.MaxFieldCost)
        {
            rejectedEstimate = new CostEstimate(summedFieldCost, summedTypeCost, null);
            limitKind = CostLimitKind.FieldCost;
            limit = _options.MaxFieldCost;
            return true;
        }

        if (summedTypeCost > _options.MaxTypeCost)
        {
            rejectedEstimate = new CostEstimate(summedFieldCost, summedTypeCost, null);
            limitKind = CostLimitKind.TypeCost;
            limit = _options.MaxTypeCost;
            return true;
        }

        if (_options.MaxResponseSize is { } maxResponseSize)
        {
            foreach (var estimate in estimates)
            {
                if (estimate.MaxResponseSize > maxResponseSize)
                {
                    rejectedEstimate = new CostEstimate(
                        summedFieldCost,
                        summedTypeCost,
                        estimate.MaxResponseSize);
                    limitKind = CostLimitKind.ResponseSize;
                    limit = maxResponseSize;
                    return true;
                }
            }
        }

        rejectedEstimate = null;
        limitKind = default;
        limit = default;
        return false;
    }

    private static async ValueTask AwaitAndReportAsync(
        RequestContext context,
        ValueTask execution,
        ImmutableArray<CostEstimate> estimates)
    {
        await execution.ConfigureAwait(false);
        context.Result = context.Result is null
            ? CostResultHelper.CreateResult(estimates)
            : CostResultHelper.AddCost(context.Result, estimates);
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
                var schemaIndex = fc.SchemaServices.GetRequiredService<CostSchemaIndex>();
                var cache = fc.SchemaServices.GetRequiredService<Cache<CostPlan>>();
                var options = fc.SchemaServices.GetRequiredService<FusionRequestOptions>().Cost;
                var diagnosticEvents =
                    fc.SchemaServices.GetRequiredService<IFusionExecutionDiagnosticEvents>();
                var middleware = new CostAnalysisMiddleware(schemaIndex, cache, options, diagnosticEvents);
                return context => middleware.InvokeAsync(context, next);
            },
            WellKnownRequestMiddleware.CostAnalyzerMiddleware);
}
