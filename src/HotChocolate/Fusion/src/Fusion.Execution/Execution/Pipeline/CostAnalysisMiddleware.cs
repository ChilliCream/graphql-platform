using System.Collections.Immutable;
using HotChocolate.Caching.Memory;
using HotChocolate.CostAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.CostAnalysis;
using HotChocolate.Fusion.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class CostAnalysisMiddleware : ICostValidationVariableCoercionPolicy
{
    private readonly CostSchemaSnapshot _snapshot;
    private readonly Cache<CostPlan> _cache;
    private readonly FusionCostOptions _options;
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;

    private CostAnalysisMiddleware(
        CostSchemaSnapshot snapshot,
        Cache<CostPlan> cache,
        FusionCostOptions options,
        IFusionExecutionDiagnosticEvents diagnosticEvents)
    {
        _snapshot = snapshot;
        _cache = cache;
        _options = options;
        _diagnosticEvents = diagnosticEvents;
    }

    public bool SkipVariableCoercion(RequestContext context)
        => !_options.SkipAnalyzer;

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

        ImmutableArray<CostEstimate> estimates;
        CostEstimate? rejectedEstimate = null;
        CostLimitKind? rejectedLimitKind = null;
        double rejectedLimit = 0;

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
                    _snapshot,
                    context.GetNormalizedDocument(),
                    operation,
                    analyses);
                _cache.TryAdd(operationId, plan);
            }

            var isStaticBound = context.IsWarmupRequest() || context.VariableValues.IsDefaultOrEmpty;

            if (isStaticBound)
            {
                var estimate = plan.EvaluateStaticBound();
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

            var analysisResult = new CostAnalysisResult(plan, estimates, isStaticBound);
            context.Features.Set(analysisResult);

            if ((mode & CostAnalysisMode.Enforce) == CostAnalysisMode.Enforce)
            {
                foreach (var estimate in estimates)
                {
                    if (estimate.FieldCost > _options.MaxFieldCost)
                    {
                        rejectedEstimate = estimate;
                        rejectedLimitKind = CostLimitKind.FieldCost;
                        rejectedLimit = _options.MaxFieldCost;
                        break;
                    }

                    if (estimate.TypeCost > _options.MaxTypeCost)
                    {
                        rejectedEstimate = estimate;
                        rejectedLimitKind = CostLimitKind.TypeCost;
                        rejectedLimit = _options.MaxTypeCost;
                        break;
                    }

                    if (_options.MaxResponseSize is { } maxResponseSize
                        && estimate.MaxResponseSize > maxResponseSize)
                    {
                        rejectedEstimate = estimate;
                        rejectedLimitKind = CostLimitKind.ResponseSize;
                        rejectedLimit = maxResponseSize;
                        break;
                    }
                }
            }
        }

        if (rejectedLimitKind is { } limitKind)
        {
            var report = (mode & CostAnalysisMode.Report) == CostAnalysisMode.Report;

            if (estimates.Length > 1)
            {
                context.Result = CostResultHelper.CreateErrorBatch(
                    estimates,
                    limitKind,
                    rejectedLimit,
                    report);
            }
            else
            {
                context.Result = CostResultHelper.CreateError(
                    rejectedEstimate!.Value,
                    limitKind,
                    rejectedLimit,
                    report);
            }

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
                var snapshot = fc.SchemaServices.GetRequiredService<CostSchemaSnapshot>();
                var cache = fc.SchemaServices.GetRequiredService<Cache<CostPlan>>();
                var options = fc.SchemaServices.GetRequiredService<FusionRequestOptions>().Cost;
                var diagnosticEvents =
                    fc.SchemaServices.GetRequiredService<IFusionExecutionDiagnosticEvents>();
                var middleware = new CostAnalysisMiddleware(snapshot, cache, options, diagnosticEvents);
                fc.Features.Set<ICostValidationVariableCoercionPolicy>(middleware);
                return context => middleware.InvokeAsync(context, next);
            },
            WellKnownRequestMiddleware.CostAnalyzerMiddleware);
}
