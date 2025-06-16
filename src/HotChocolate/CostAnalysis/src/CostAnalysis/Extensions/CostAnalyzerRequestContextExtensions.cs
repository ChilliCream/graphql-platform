using HotChocolate.CostAnalysis;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

/// <summary>
/// Cost Analyzer extensions for the <see cref="RequestContext"/>.
/// </summary>
public static class CostAnalyzerRequestContextExtensions
{
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
        RequestCostOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        if (options.SkipAnalyzer)
        {
            return CostAnalyzerMode.Skip;
        }

        if (context.ContextData.ContainsKey(WellKnownContextData.ValidateCost))
        {
            return CostAnalyzerMode.Analyze | CostAnalyzerMode.Report;
        }

        var flags = CostAnalyzerMode.Analyze;

        if (options.EnforceCostLimits)
        {
            flags |= CostAnalyzerMode.Enforce;
        }

        flags |= CostAnalyzerMode.Execute;

        if (context.ContextData.ContainsKey(WellKnownContextData.ReportCost))
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
    public static OperationRequestBuilder SetCostOptions(
        this OperationRequestBuilder builder,
        RequestCostOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        builder.Features.Set(options);
        return builder;
    }
}
