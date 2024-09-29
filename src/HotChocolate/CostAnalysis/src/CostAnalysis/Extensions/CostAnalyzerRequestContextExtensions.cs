using HotChocolate.CostAnalysis;

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
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(costMetrics);

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
        ArgumentNullException.ThrowIfNull(context);

        if (context.ContextData.TryGetValue(WellKnownContextData.CostMetrics, out var value) &&
            value is CostMetrics costMetrics)
        {
            return costMetrics;
        }

        return new CostMetrics();
    }

    internal static CostAnalyzerMode GetCostAnalyzerMode(
        this IRequestContext context,
        CostOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

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
}
