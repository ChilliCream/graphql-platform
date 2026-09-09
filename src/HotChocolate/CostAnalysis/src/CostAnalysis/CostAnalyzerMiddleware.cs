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
            context.Result = ResultHelper.StateInvalidForCostAnalysis();
            return;
        }

        CostMetrics costMetrics;

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

                var first = estimates[0];
                costMetrics = new CostMetrics
                {
                    FieldCost = first.FieldCost,
                    TypeCost = first.TypeCost
                };
                context.SetCostMetrics(costMetrics);

                if ((mode & CostAnalyzerMode.Enforce) == CostAnalyzerMode.Enforce
                    && !TryEnforce(context, requestOptions, mode, estimates))
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
                    ? costMetrics.CreateResult()
                    : context.Result.AddCostMetrics(costMetrics);
        }
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
        ImmutableArray<CostEstimate> estimates)
    {
        var reportMetrics = (mode & CostAnalyzerMode.Report) == CostAnalyzerMode.Report;

        foreach (var estimate in estimates)
        {
            var costMetrics = new CostMetrics
            {
                FieldCost = estimate.FieldCost,
                TypeCost = estimate.TypeCost
            };

            if (estimate.FieldCost > requestOptions.MaxFieldCost)
            {
                context.Result = ErrorHelper.MaxFieldCostReached(
                    costMetrics,
                    requestOptions.MaxFieldCost,
                    reportMetrics);
                return false;
            }

            if (estimate.TypeCost > requestOptions.MaxTypeCost)
            {
                context.Result = ErrorHelper.MaxTypeCostReached(
                    costMetrics,
                    requestOptions.MaxTypeCost,
                    reportMetrics);
                return false;
            }

            if (requestOptions.MaxResponseSize is { } maxResponseSize
                && estimate.MaxResponseSize is { } responseSize
                && responseSize > maxResponseSize)
            {
                context.Result = ErrorHelper.MaxResponseSizeReached(
                    costMetrics,
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
