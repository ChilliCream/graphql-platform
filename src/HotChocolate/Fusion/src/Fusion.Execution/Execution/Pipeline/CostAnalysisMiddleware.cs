using System.Collections.Immutable;
using HotChocolate.Caching.Memory;
using HotChocolate.CostAnalysis;
using HotChocolate.Execution;
using HotChocolate.Execution.Pipeline;
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
        var requestOptions = context.TryGetCostOptions();
        var effectiveOptions = requestOptions is null
            ? new EffectiveCostOptions(_options)
            : new EffectiveCostOptions(requestOptions);
        var mode = GetMode(context, effectiveOptions);

        if (mode == CostAnalysisMode.Skip)
        {
            return next(context);
        }

        // A request can override the response-size limit only if the gateway enables the analysis.
        if (requestOptions?.MaxResponseSize.HasValue == true && !_options.MaxResponseSize.HasValue)
        {
            context.Result = ErrorHelper.ResponseSizeAnalysisNotEnabled();
            return default;
        }

        // Cost analysis requires at least one coerced variable set, so an explicit empty variable batch is invalid.
        if (context.VariableValues.IsDefaultOrEmpty)
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
                    context.GetNormalizedOperation(),
                    analyses);
                _cache.TryAdd(operationId, plan);
            }

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

            var analysisResult = new CostAnalysisResult(plan, estimates);
            context.Features.Set(analysisResult);

            if ((mode & CostAnalysisMode.Enforce) == CostAnalysisMode.Enforce)
            {
                if (estimates.Length == 1)
                {
                    TryGetViolation(
                        estimates[0],
                        effectiveOptions,
                        out rejectedEstimate,
                        out rejectedLimitKind,
                        out rejectedLimit);
                }
                else
                {
                    TryGetBatchViolation(
                        estimates,
                        effectiveOptions,
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

    private static bool TryGetViolation(
        CostEstimate estimate,
        EffectiveCostOptions options,
        out CostEstimate? rejectedEstimate,
        out CostLimitKind limitKind,
        out double limit)
    {
        if (estimate.FieldCost > options.MaxFieldCost)
        {
            rejectedEstimate = estimate;
            limitKind = CostLimitKind.FieldCost;
            limit = options.MaxFieldCost;
            return true;
        }

        if (estimate.TypeCost > options.MaxTypeCost)
        {
            rejectedEstimate = estimate;
            limitKind = CostLimitKind.TypeCost;
            limit = options.MaxTypeCost;
            return true;
        }

        if (options.MaxResponseSize is { } maxResponseSize
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

    private static bool TryGetBatchViolation(
        ImmutableArray<CostEstimate> estimates,
        EffectiveCostOptions options,
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

        if (summedFieldCost > options.MaxFieldCost)
        {
            rejectedEstimate = new CostEstimate(summedFieldCost, summedTypeCost, null);
            limitKind = CostLimitKind.FieldCost;
            limit = options.MaxFieldCost;
            return true;
        }

        if (summedTypeCost > options.MaxTypeCost)
        {
            rejectedEstimate = new CostEstimate(summedFieldCost, summedTypeCost, null);
            limitKind = CostLimitKind.TypeCost;
            limit = options.MaxTypeCost;
            return true;
        }

        if (options.MaxResponseSize is { } maxResponseSize)
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

    private static CostAnalysisMode GetMode(RequestContext context, EffectiveCostOptions options)
    {
        if (options.SkipAnalyzer)
        {
            return CostAnalysisMode.Skip;
        }

        if (context.ContextData.ContainsKey(ExecutionContextData.ValidateCost))
        {
            return CostAnalysisMode.Analyze | CostAnalysisMode.Report;
        }

        var mode = CostAnalysisMode.Analyze | CostAnalysisMode.Execute;

        if (options.EnforceCostLimits)
        {
            mode |= CostAnalysisMode.Enforce;
        }

        if (context.ContextData.ContainsKey(ExecutionContextData.ReportCost))
        {
            mode |= CostAnalysisMode.Report;
        }

        return mode;
    }

    /// <summary>
    /// The cost limits in effect for one request: the request-level <see cref="FusionRequestCostOptions"/>
    /// when the request set one, otherwise the gateway's <see cref="FusionCostOptions"/>.
    /// </summary>
    private readonly record struct EffectiveCostOptions(
        double MaxFieldCost,
        double MaxTypeCost,
        bool EnforceCostLimits,
        bool SkipAnalyzer,
        double? MaxResponseSize)
    {
        public EffectiveCostOptions(FusionCostOptions options)
            : this(
                options.MaxFieldCost,
                options.MaxTypeCost,
                options.EnforceCostLimits,
                options.SkipAnalyzer,
                options.MaxResponseSize)
        {
        }

        public EffectiveCostOptions(FusionRequestCostOptions options)
            : this(
                options.MaxFieldCost,
                options.MaxTypeCost,
                options.EnforceCostLimits,
                options.SkipAnalyzer,
                options.MaxResponseSize)
        {
        }
    }

    public static RequestMiddlewareConfiguration Create()
        => new(
            (fc, next) =>
            {
                var schemaIndex = fc.SchemaServices.GetRequiredService<CostSchemaIndex>();
                var cache = fc.SchemaServices.GetRequiredService<Cache<CostPlan>>();
                var options = fc.SchemaServices.GetRequiredService<FusionCostOptions>();
                var diagnosticEvents =
                    fc.SchemaServices.GetRequiredService<IFusionExecutionDiagnosticEvents>();
                var middleware = new CostAnalysisMiddleware(schemaIndex, cache, options, diagnosticEvents);
                return context => middleware.InvokeAsync(context, next);
            },
            WellKnownRequestMiddleware.CostAnalyzerMiddleware);
}
