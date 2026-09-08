using static HotChocolate.CostAnalysis.Properties.CostAnalysisCoreResources;

namespace HotChocolate.CostAnalysis;

internal static class ThrowHelper
{
    public static NotImplementedException NotImplemented()
        => new(ThrowHelper_NotImplemented);
}
