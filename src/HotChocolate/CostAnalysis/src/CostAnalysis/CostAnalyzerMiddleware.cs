using System.Collections.Immutable;
using HotChocolate.CostAnalysis.Utilities;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using ErrorHelper = HotChocolate.CostAnalysis.Utilities.ErrorHelper;

namespace HotChocolate.CostAnalysis;

internal sealed class CostAnalyzerMiddleware(
    RequestDelegate next,
    [SchemaService] RequestCostOptions options,
    [SchemaService] CostSchemaSnapshot snapshot,
    [SchemaService] CostPlanCache cache,
    ObjectPool<DocumentValidatorContext> contextPool,
    [SchemaService] IExecutionDiagnosticEvents diagnosticEvents)
    : ICostValidationVariableCoercionPolicy
{
    public bool SkipVariableCoercion(RequestContext context)
        => !(context.TryGetCostOptions() ?? options).SkipAnalyzer;

    public async ValueTask InvokeAsync(RequestContext context)
    {
        var requestOptions = context.TryGetCostOptions() ?? options;
        var mode = context.GetCostAnalyzerMode(requestOptions);

        if (mode == CostAnalyzerMode.Skip)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (!context.TryGetOperation(out var operation)
            || !context.TryGetOperationDocument(out var document, out var documentId)
            || documentId.IsEmpty)
        {
            context.Result = ErrorHelper.StateInvalidForCostAnalysis();
            return;
        }

        ImmutableArray<CostMetrics> costMetrics;

        using (diagnosticEvents.AnalyzeOperationCost(context))
        {
            try
            {
                if (!cache.TryGetPlan(operation.Id, out var plan))
                {
                    var analyses = CostAnalyses.Cost;

                    if (options.MaxResponseSize.HasValue)
                    {
                        analyses |= CostAnalyses.ResponseSize;
                    }

                    plan = CostPlanCompiler.Compile(
                        snapshot,
                        operation.Document,
                        operation.Definition,
                        analyses);
                    cache.TryAddPlan(operation.Id, plan);
                }

                CostAnalyzerUtilities.ValidateRequireOneSlicingArgument(
                    operation,
                    document,
                    documentId,
                    context.Features,
                    contextPool);

                var isStaticBound = context.IsWarmupRequest() || context.VariableValues.Length == 0;
                var estimates = Evaluate(context, plan, isStaticBound);
                context.Features.Set(new CostAnalysisResult(plan, estimates, isStaticBound));

                costMetrics = CreateCostMetrics(estimates);
                context.SetCostMetrics(costMetrics[0]);

                if ((mode & CostAnalyzerMode.Enforce) == CostAnalyzerMode.Enforce
                    && !TryEnforce(context, requestOptions, mode, costMetrics))
                {
                    return;
                }
            }
            catch (GraphQLException ex)
            {
                context.Result = ResultHelper.CreateError(ex.Errors, null);
                return;
            }
        }

        if ((mode & CostAnalyzerMode.Execute) == CostAnalyzerMode.Execute)
        {
            await next(context).ConfigureAwait(false);
        }

        if ((mode & CostAnalyzerMode.Report) == CostAnalyzerMode.Report)
        {
            context.Result =
                context.Result is null
                    ? costMetrics[0].CreateResult()
                    : context.Result.AddCostMetrics(costMetrics);
        }
    }

    private static ImmutableArray<CostMetrics> CreateCostMetrics(
        ImmutableArray<CostEstimate> estimates)
    {
        var builder = ImmutableArray.CreateBuilder<CostMetrics>(estimates.Length);

        foreach (var estimate in estimates)
        {
            builder.Add(
                new CostMetrics
                {
                    FieldCost = estimate.FieldCost,
                    TypeCost = estimate.TypeCost,
                    MaxResponseSize = estimate.MaxResponseSize
                });
        }

        return builder.MoveToImmutable();
    }

    private ImmutableArray<CostEstimate> Evaluate(
        RequestContext context,
        CostPlan plan,
        bool isStaticBound)
    {
        if (isStaticBound)
        {
            var estimate = plan.EvaluateStaticBound();
            diagnosticEvents.OperationCost(context, estimate.FieldCost, estimate.TypeCost);
            return [estimate];
        }

        var builder = ImmutableArray.CreateBuilder<CostEstimate>(context.VariableValues.Length);

        foreach (var variableValues in context.VariableValues)
        {
            var estimate = plan.Evaluate(new CostVariableValuesAdapter(variableValues));
            builder.Add(estimate);
            diagnosticEvents.OperationCost(context, estimate.FieldCost, estimate.TypeCost);
        }

        return builder.MoveToImmutable();
    }

    private static bool TryEnforce(
        RequestContext context,
        RequestCostOptions requestOptions,
        CostAnalyzerMode mode,
        ImmutableArray<CostMetrics> costMetrics)
    {
        var reportMetrics = (mode & CostAnalyzerMode.Report) == CostAnalyzerMode.Report;

        foreach (var current in costMetrics)
        {
            if (current.FieldCost > requestOptions.MaxFieldCost)
            {
                context.Result = ErrorHelper.MaxFieldCostReached(
                    current,
                    requestOptions.MaxFieldCost,
                    reportMetrics);
                return false;
            }

            if (current.TypeCost > requestOptions.MaxTypeCost)
            {
                context.Result = ErrorHelper.MaxTypeCostReached(
                    current,
                    requestOptions.MaxTypeCost,
                    reportMetrics);
                return false;
            }

            if (requestOptions.MaxResponseSize is { } maxResponseSize
                && current.MaxResponseSize is { } responseSize
                && responseSize > maxResponseSize)
            {
                context.Result = ErrorHelper.MaxResponseSizeReached(
                    current,
                    responseSize,
                    maxResponseSize,
                    reportMetrics);
                return false;
            }
        }

        return true;
    }

    public static RequestMiddlewareConfiguration Create()
    {
        return new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var options = core.SchemaServices.GetRequiredService<RequestCostOptions>();
                var snapshot = core.SchemaServices.GetRequiredService<CostSchemaSnapshot>();
                var cache = core.SchemaServices.GetRequiredService<CostPlanCache>();
                var contextPool = core.Services.GetRequiredService<ObjectPool<DocumentValidatorContext>>();
                var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();

                var middleware = new CostAnalyzerMiddleware(
                    next,
                    options,
                    snapshot,
                    cache,
                    contextPool,
                    diagnosticEvents);
                core.Features.Set<ICostValidationVariableCoercionPolicy>(middleware);

                return context => middleware.InvokeAsync(context);
            },
            WellKnownRequestMiddleware.CostAnalyzerMiddleware);
    }
}
