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
    [SchemaService] CostOptions costOptions,
    [SchemaService] Schema schema,
    [SchemaService] CostSchemaIndex schemaIndex,
    [SchemaService] CostPlanCache cache,
    ObjectPool<DocumentValidatorContext> contextPool,
    [SchemaService] IExecutionDiagnosticEvents diagnosticEvents)
{
    public async ValueTask InvokeAsync(RequestContext context)
    {
        var requestOptions = ResolveRequestOptions(context);
        var mode = context.GetCostAnalyzerMode(requestOptions.SkipAnalyzer, requestOptions.EnforceCostLimits);

        if (mode == CostAnalyzerMode.Skip)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        // A request can override the response-size limit only if the schema enables the analysis.
        if (requestOptions.MaxResponseSize.HasValue && !costOptions.MaxResponseSize.HasValue)
        {
            context.Result = ErrorHelper.ResponseSizeAnalysisNotEnabled();
            return;
        }

        if (!context.TryGetOperationDocument(out var document, out var documentId) || documentId.IsEmpty)
        {
            context.Result = ErrorHelper.StateInvalidForCostAnalysis();
            return;
        }

        // Cost analysis runs before the operation cache, so the operation id may not be set yet.
        var operationId = context.GetOperationId();

        ImmutableArray<CostMetrics> costMetrics;

        using (diagnosticEvents.AnalyzeOperationCost(context))
        {
            try
            {
                if (!cache.TryGetPlan(operationId, out var plan))
                {
                    // Validate before caching the plan so retries cannot bypass a validation error.
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

                    if (costOptions.MaxResponseSize.HasValue)
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

                // Cost analysis requires at least one coerced variable set, so an explicit empty
                // variable batch is invalid.
                if (context.VariableValues.Length == 0)
                {
                    context.Result = ErrorHelper.StateInvalidForCostAnalysisMissingVariableValues();
                    return;
                }

                var estimates = Evaluate(context, plan);
                context.Features.Set(new CostAnalysisResult(plan, estimates));

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
        CostPlan plan)
    {
        var builder = ImmutableArray.CreateBuilder<CostEstimate>(context.VariableValues.Length);

        foreach (var variableValues in context.VariableValues)
        {
            var estimate = plan.Evaluate(new CostVariableValuesAdapter(variableValues));
            builder.Add(estimate);
            diagnosticEvents.OperationCost(context, estimate.FieldCost, estimate.TypeCost);
        }

        return builder.MoveToImmutable();
    }

    /// <summary>
    /// Resolves the effective cost options for the request: a copy of the schema's cost options
    /// with the legacy <see cref="RequestCostOptions"/> value (if the request set one) applied,
    /// followed by every ModifyCostOptions modifier added to the request, in order.
    /// </summary>
    private CostOptions ResolveRequestOptions(RequestContext context)
    {
        var legacyOptions = context.TryGetCostOptions();
        var hasModifiers = context.TryGetCostOptionsModifiers(out var modifiers);

        if (legacyOptions is null && !hasModifiers)
        {
            return costOptions;
        }

        var effectiveOptions = costOptions.Copy();

        if (legacyOptions is not null)
        {
            effectiveOptions.MaxFieldCost = legacyOptions.MaxFieldCost;
            effectiveOptions.MaxTypeCost = legacyOptions.MaxTypeCost;
            effectiveOptions.EnforceCostLimits = legacyOptions.EnforceCostLimits;
            effectiveOptions.SkipAnalyzer = legacyOptions.SkipAnalyzer;
            effectiveOptions.MaxResponseSize = legacyOptions.MaxResponseSize;
        }

        modifiers?.Apply(effectiveOptions);

        return effectiveOptions;
    }

    private static bool TryCreateVariableBatchEnforcementError(
        CostOptions requestOptions,
        ImmutableArray<CostMetrics> costMetrics,
        bool reportMetrics,
        [NotNullWhen(true)]
        out IExecutionResult? error)
    {
        // A variable batch shares one field-cost limit and one type-cost limit across all items.
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
        CostOptions requestOptions,
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
                var costOptions = core.SchemaServices.GetRequiredService<CostOptions>();
                var schema = core.SchemaServices.GetRequiredService<Schema>();
                var schemaIndex = core.SchemaServices.GetRequiredService<CostSchemaIndex>();
                var cache = core.SchemaServices.GetRequiredService<CostPlanCache>();
                var contextPool = core.Services.GetRequiredService<ObjectPool<DocumentValidatorContext>>();
                var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();

                var middleware = new CostAnalyzerMiddleware(
                    next,
                    costOptions,
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
