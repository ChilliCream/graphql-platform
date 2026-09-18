using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
    [SchemaService] Schema schema,
    [SchemaService] CostSchemaIndex schemaIndex,
    [SchemaService] CostPlanCache cache,
    ObjectPool<DocumentValidatorContext> contextPool,
    [SchemaService] IExecutionDiagnosticEvents diagnosticEvents)
{
    public async ValueTask InvokeAsync(RequestContext context)
    {
        var requestOptions = context.TryGetCostOptions() ?? options;
        var mode = context.GetCostAnalyzerMode(requestOptions);

        if (mode == CostAnalyzerMode.Skip)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        // A request-level override only ever replaces a limit the schema already enforces
        // (in either direction). If the schema never enabled the response-size analysis,
        // honoring the override would silently promise a check that never runs, so this
        // fails fast instead.
        if (requestOptions.MaxResponseSize.HasValue && !options.MaxResponseSize.HasValue)
        {
            context.Result = ErrorHelper.ResponseSizeAnalysisNotEnabled();
            return;
        }

        if (!context.TryGetOperationId(out var operationId)
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
                if (!cache.TryGetPlan(operationId, out var plan))
                {
                    // The document is normalized here, and nowhere else on this path, because a
                    // plan cache hit means an earlier request already normalized, planned, and
                    // validated the very same operation shape. Validation runs before the plan
                    // is cached so a violation is never masked by a plan a failed request left
                    // behind: it keeps missing, and keeps failing, on every retry.
                    var normalizedDocument = context.GetNormalizedDocument();
                    var normalizedOperation = context.GetNormalizedOperation();

                    CostAnalyzerUtilities.ValidateRequireOneSlicingArgument(
                        schema,
                        normalizedOperation,
                        normalizedDocument,
                        document,
                        documentId,
                        context.Features,
                        contextPool);

                    var analyses = CostAnalyses.Cost;

                    if (options.MaxResponseSize.HasValue)
                    {
                        analyses |= CostAnalyses.ResponseSize;
                    }

                    plan = CostPlanCompiler.Compile(
                        schemaIndex,
                        normalizedDocument,
                        normalizedOperation,
                        analyses);
                    cache.TryAddPlan(operationId, plan);
                }

                var isAssumedBound = context.IsWarmupRequest();

                // Non-warmup cost analysis requires at least one coerced variable set, so an explicit empty variable batch is invalid.
                if (!isAssumedBound && context.VariableValues.Length == 0)
                {
                    context.Result = ErrorHelper.StateInvalidForCostAnalysisMissingVariableValues();
                    return;
                }

                var estimates = Evaluate(context, plan, isAssumedBound);
                context.Features.Set(new CostAnalysisResult(plan, estimates, isAssumedBound));

                costMetrics = CreateCostMetrics(estimates);
                context.SetCostMetrics(costMetrics[0]);

                if ((mode & CostAnalyzerMode.Enforce) == CostAnalyzerMode.Enforce)
                {
                    if (costMetrics.Length == 1)
                    {
                        if (TryCreateEnforcementError(
                            requestOptions,
                            costMetrics[0],
                            (mode & CostAnalyzerMode.Report) == CostAnalyzerMode.Report,
                            out var error))
                        {
                            context.Result = error;
                            return;
                        }
                    }
                    else if (TryCreateVariableBatchEnforcementError(
                        requestOptions,
                        costMetrics,
                        (mode & CostAnalyzerMode.Report) == CostAnalyzerMode.Report,
                        out var error))
                    {
                        context.Result = error;
                        return;
                    }
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
        bool isAssumedBound)
    {
        if (isAssumedBound)
        {
            var estimate = plan.EvaluateAssumedBound();
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

    private static bool TryCreateVariableBatchEnforcementError(
        RequestCostOptions requestOptions,
        ImmutableArray<CostMetrics> costMetrics,
        bool reportMetrics,
        [NotNullWhen(true)]
        out IExecutionResult? error)
    {
        // A request is one invocation of the request pipeline. A variable batch is one request,
        // so its allowed field and type cost is the sum over every variable set.
        var fieldCost = 0d;
        var typeCost = 0d;

        foreach (var current in costMetrics)
        {
            fieldCost += current.FieldCost;
            typeCost += current.TypeCost;
        }

        var requestCostMetrics = new CostMetrics
        {
            FieldCost = fieldCost,
            TypeCost = typeCost
        };

        if (requestCostMetrics.FieldCost > requestOptions.MaxFieldCost)
        {
            error = ErrorHelper.MaxFieldCostReached(
                requestCostMetrics,
                requestOptions.MaxFieldCost,
                reportMetrics);
            return true;
        }

        if (requestCostMetrics.TypeCost > requestOptions.MaxTypeCost)
        {
            error = ErrorHelper.MaxTypeCostReached(
                requestCostMetrics,
                requestOptions.MaxTypeCost,
                reportMetrics);
            return true;
        }

        if (requestOptions.MaxResponseSize is { } maxResponseSize)
        {
            foreach (var current in costMetrics)
            {
                if (current.MaxResponseSize is { } responseSize
                    && responseSize > maxResponseSize)
                {
                    error = ErrorHelper.MaxResponseSizeReached(
                        requestCostMetrics with { MaxResponseSize = responseSize },
                        responseSize,
                        maxResponseSize,
                        reportMetrics);
                    return true;
                }
            }
        }

        error = null;
        return false;
    }

    private static bool TryCreateEnforcementError(
        RequestCostOptions requestOptions,
        CostMetrics costMetrics,
        bool reportMetrics,
        [NotNullWhen(true)]
        out IExecutionResult? error)
    {
        if (costMetrics.FieldCost > requestOptions.MaxFieldCost)
        {
            error = ErrorHelper.MaxFieldCostReached(
                costMetrics,
                requestOptions.MaxFieldCost,
                reportMetrics);
            return true;
        }

        if (costMetrics.TypeCost > requestOptions.MaxTypeCost)
        {
            error = ErrorHelper.MaxTypeCostReached(
                costMetrics,
                requestOptions.MaxTypeCost,
                reportMetrics);
            return true;
        }

        if (requestOptions.MaxResponseSize is { } maxResponseSize
            && costMetrics.MaxResponseSize is { } responseSize
            && responseSize > maxResponseSize)
        {
            error = ErrorHelper.MaxResponseSizeReached(
                costMetrics,
                responseSize,
                maxResponseSize,
                reportMetrics);
            return true;
        }

        error = null;
        return false;
    }

    public static RequestMiddlewareConfiguration Create()
    {
        return new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var options = core.SchemaServices.GetRequiredService<RequestCostOptions>();
                var schema = core.SchemaServices.GetRequiredService<Schema>();
                var schemaIndex = core.SchemaServices.GetRequiredService<CostSchemaIndex>();
                var cache = core.SchemaServices.GetRequiredService<CostPlanCache>();
                var contextPool = core.Services.GetRequiredService<ObjectPool<DocumentValidatorContext>>();
                var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();

                var middleware = new CostAnalyzerMiddleware(
                    next,
                    options,
                    schema,
                    schemaIndex,
                    cache,
                    contextPool,
                    diagnosticEvents);

                return context => middleware.InvokeAsync(context);
            },
            WellKnownRequestMiddleware.CostAnalyzerMiddleware);
    }
}
