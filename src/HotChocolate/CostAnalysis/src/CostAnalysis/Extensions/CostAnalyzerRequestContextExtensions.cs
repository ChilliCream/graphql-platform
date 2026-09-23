using System.Diagnostics.CodeAnalysis;
using HotChocolate.CostAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

/// <summary>
/// Cost Analyzer extensions for the <see cref="RequestContext"/>.
/// </summary>
public static class CostAnalyzerRequestContextExtensions
{
    /// <summary>
    /// Attempts to get the compiled cost plan and estimates for this request.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <param name="result">
    /// The cost analysis result, or <see langword="null"/> when no result is available.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the request has a cost analysis result.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    public static bool TryGetCostAnalysisResult(
        this RequestContext context,
        [NotNullWhen(true)] out CostAnalysisResult? result)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Features.TryGet(out result);
    }

    internal static RequestContext SetCostMetrics(
        this RequestContext context,
        CostMetrics costMetrics)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(costMetrics);

        context.Features.Set(costMetrics);
        return context;
    }

    /// <summary>
    /// Gets the cost metrics from the context.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <returns>
    /// Returns the cost metrics.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> is <c>null</c>.
    /// </exception>
    public static CostMetrics GetCostMetrics(
        this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Features.TryGet<CostMetrics>(out var costMetrics))
        {
            return costMetrics;
        }

        return new CostMetrics();
    }

    internal static CostAnalyzerMode GetCostAnalyzerMode(
        this RequestContext context,
        bool skipAnalyzer,
        bool enforceCostLimits)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (skipAnalyzer)
        {
            return CostAnalyzerMode.Skip;
        }

        if (context.ContextData.ContainsKey(ExecutionContextData.ValidateCost))
        {
            return CostAnalyzerMode.Analyze | CostAnalyzerMode.Report;
        }

        var flags = CostAnalyzerMode.Analyze;

        if (enforceCostLimits)
        {
            flags |= CostAnalyzerMode.Enforce;
        }

        flags |= CostAnalyzerMode.Execute;

        if (context.ContextData.ContainsKey(ExecutionContextData.ReportCost))
        {
            flags |= CostAnalyzerMode.Report;
        }

        return flags;
    }

    /// <summary>
    /// Gets the cost options for the current request.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <returns>
    /// Returns the cost options.
    /// </returns>
#pragma warning disable CS0618 // RequestCostOptions and the members below are obsolete but kept for backward compatibility.
    [Obsolete(
        "Use RequestContext.TryGetCostAnalysisResult to read the cost analysis outcome, or "
        + "ModifyCostOptions to change the per-request cost options. Removed in 17.0. This "
        + "getter never reflects ModifyCostOptions modifiers.")]
    public static RequestCostOptions GetCostOptions(this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Features.TryGet<RequestCostOptions>(out var options))
        {
            return options;
        }

        return context.Schema.Services.GetRequiredService<RequestCostOptions>();
    }

    /// <summary>
    /// Gets the global cost options from the executor.
    /// </summary>
    /// <param name="executor">
    /// The GraphQL executor.
    /// </param>
    /// <returns>
    /// Returns the global cost options.
    /// </returns>
    [Obsolete(
        "Use RequestContext.TryGetCostAnalysisResult to read the cost analysis outcome, or "
        + "ModifyCostOptions to change the per-request cost options. Removed in 17.0. This "
        + "getter never reflects ModifyCostOptions modifiers.")]
    public static RequestCostOptions GetCostOptions(this IRequestExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(executor);

        return executor.Schema.Services.GetRequiredService<RequestCostOptions>();
    }

    internal static RequestCostOptions? TryGetCostOptions(this RequestContext context)
    {
        if (context.Features.TryGet<RequestCostOptions>(out var options))
        {
            return options;
        }

        return null;
    }

    /// <summary>
    /// Sets the cost options for the current request.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <param name="options">
    /// The cost options.
    /// </param>
    [Obsolete(
        "Use ModifyCostOptions(Action<CostOptions>) instead. Removed in 17.0. If the request "
        + "also has ModifyCostOptions modifiers, the modifiers are applied on top of this value.")]
    public static void SetCostOptions(this RequestContext context, RequestCostOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        context.Features.Set(options);
    }

    /// <summary>
    /// Sets the cost options for the current request.
    /// </summary>
    /// <param name="builder">
    /// The operation request builder.
    /// </param>
    /// <param name="options">
    /// The cost options.
    /// </param>
    /// <returns>
    /// Returns the operation request builder.
    /// </returns>
    [Obsolete(
        "Use ModifyCostOptions(Action<CostOptions>) instead. Removed in 17.0. If the request "
        + "also has ModifyCostOptions modifiers, the modifiers are applied on top of this value.")]
    public static OperationRequestBuilder SetCostOptions(
        this OperationRequestBuilder builder,
        RequestCostOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        builder.Features.Set(options);
        return builder;
    }
#pragma warning restore CS0618

    /// <summary>
    /// Adds a modifier that mutates a per-request copy of the schema's cost options.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <param name="configure">
    /// A delegate that mutates the per-request cost options. Only
    /// <see cref="CostOptions.MaxFieldCost"/>, <see cref="CostOptions.MaxTypeCost"/>,
    /// <see cref="CostOptions.EnforceCostLimits"/>, <see cref="CostOptions.SkipAnalyzer"/>,
    /// and <see cref="CostOptions.MaxResponseSize"/> apply per request, changes to any other
    /// option have no effect. Modifiers added to the same request run in the order they were
    /// added.
    /// </param>
    /// <returns>
    /// Returns the request context.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> or <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    public static RequestContext ModifyCostOptions(
        this RequestContext context,
        Action<CostOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(configure);

        context.Features.Set(
            context.Features.TryGet<CostOptionsModifiers>(out var existing)
                ? existing.With(configure)
                : new CostOptionsModifiers(configure));
        return context;
    }

    /// <summary>
    /// Adds a modifier that mutates a per-request copy of the schema's cost options.
    /// </summary>
    /// <param name="builder">
    /// The operation request builder.
    /// </param>
    /// <param name="configure">
    /// A delegate that mutates the per-request cost options. Only
    /// <see cref="CostOptions.MaxFieldCost"/>, <see cref="CostOptions.MaxTypeCost"/>,
    /// <see cref="CostOptions.EnforceCostLimits"/>, <see cref="CostOptions.SkipAnalyzer"/>,
    /// and <see cref="CostOptions.MaxResponseSize"/> apply per request, changes to any other
    /// option have no effect. Modifiers added to the same request run in the order they were
    /// added.
    /// </param>
    /// <returns>
    /// Returns the operation request builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    public static OperationRequestBuilder ModifyCostOptions(
        this OperationRequestBuilder builder,
        Action<CostOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Features.Set(
            builder.Features.TryGet<CostOptionsModifiers>(out var existing)
                ? existing.With(configure)
                : new CostOptionsModifiers(configure));
        return builder;
    }

    internal static bool TryGetCostOptionsModifiers(
        this RequestContext context,
        [NotNullWhen(true)] out CostOptionsModifiers? modifiers)
        => context.Features.TryGet(out modifiers);
}
