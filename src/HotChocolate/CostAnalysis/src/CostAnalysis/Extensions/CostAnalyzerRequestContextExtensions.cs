using HotChocolate.CostAnalysis;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

/// <summary>
/// Cost Analyzer extensions for the <see cref="IRequestContext"/>.
/// </summary>
public static class CostAnalyzerRequestContextExtensions
{
    internal static IRequestContext AddCostMetrics(
        this IRequestContext context,
        CostMetrics costMetrics)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (costMetrics is null)
        {
            throw new ArgumentNullException(nameof(costMetrics));
        }

        context.ContextData[WellKnownContextData.CostMetrics] = costMetrics;
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
        this IRequestContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.ContextData.TryGetValue(WellKnownContextData.CostMetrics, out var value) &&
            value is CostMetrics costMetrics)
        {
            return costMetrics;
        }

        return new CostMetrics();
    }

    internal static CostAnalyzerMode GetCostAnalyzerMode(
        this IRequestContext context,
        bool enforceCostLimits)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.ContextData.ContainsKey(WellKnownContextData.ValidateCost))
        {
            return CostAnalyzerMode.Analyze | CostAnalyzerMode.Report;
        }

        var flags = CostAnalyzerMode.Analyze;

        if (enforceCostLimits)
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
    public static RequestCostOptions GetCostOptions(this IRequestContext context)
    {
        if (context.ContextData.TryGetValue(WellKnownContextData.RequestCostOptions, out var value)
            && value is RequestCostOptions options)
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
        return executor.Schema.Services.GetRequiredService<RequestCostOptions>();
    }

    internal static RequestCostOptions? TryGetCostOptions(this IRequestContext context)
    {
        if (context.ContextData.TryGetValue(WellKnownContextData.RequestCostOptions, out var value)
            && value is RequestCostOptions options)
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
    public static void SetCostOptions(this IRequestContext context, RequestCostOptions options)
    {
        context.ContextData[WellKnownContextData.RequestCostOptions] = options;
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
        => builder.SetGlobalState(WellKnownContextData.RequestCostOptions, options);
}
