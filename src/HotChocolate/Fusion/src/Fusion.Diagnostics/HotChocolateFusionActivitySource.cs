using System.Diagnostics;
using HotChocolate.Fusion.Diagnostics.Listeners;

namespace HotChocolate.Fusion.Diagnostics;

internal static class HotChocolateFusionActivitySource
{
    public static ActivitySource Source { get; } = new(GetName(), GetVersion());

    public static string GetName()
        => typeof(FusionActivityExecutionDiagnosticEventListener).Assembly.GetName().Name!;

    private static string GetVersion()
        => typeof(FusionActivityExecutionDiagnosticEventListener).Assembly.GetName().Version!.ToString();
}
