using System.Diagnostics;
using HotChocolate.Fusion.Diagnostics.Listeners;

namespace HotChocolate.Fusion.Diagnostics;

internal static class HotChocolateFusionActivitySource
{
    public static ActivitySource Source { get; } = new(GetName(), GetVersion());

    public static string GetName()
        => typeof(ActivityFusionExecutionDiagnosticEventListener).Assembly.GetName().Name!;

    private static string GetVersion()
        => typeof(ActivityFusionExecutionDiagnosticEventListener).Assembly.GetName().Version!.ToString();
}
