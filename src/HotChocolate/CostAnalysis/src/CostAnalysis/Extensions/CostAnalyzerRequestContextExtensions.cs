using HotChocolate.CostAnalysis;

namespace HotChocolate.Execution;

internal static class CostAnalyzerRequestContextExtensions
{
    public static IRequestContext AddCostMetrics(
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

    public static CostAnalyzerMode GetCostAnalyzerMode(
        this IRequestContext context,
        CostOptions options)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (context.ContextData.ContainsKey(WellKnownContextData.ValidateCost))
        {
            return CostAnalyzerMode.ValidateAndReport;
        }

        if (!options.EnforceCostLimits)
        {
            return CostAnalyzerMode.Analysis;
        }

        if (context.ContextData.ContainsKey(WellKnownContextData.ReporCostInResponse))
        {
            return CostAnalyzerMode.EnforceAndReport;
        }

        return CostAnalyzerMode.Enforce;
    }
}
