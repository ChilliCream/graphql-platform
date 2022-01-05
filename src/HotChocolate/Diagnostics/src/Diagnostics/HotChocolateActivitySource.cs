using System.Diagnostics;
using HotChocolate.Diagnostics.Listeners;

namespace HotChocolate.Diagnostics;

internal static class HotChocolateActivitySource
{
    public static ActivitySource Source { get; } = new(GetName(), GetVersion());

    public static string GetName()
        => typeof(ActivityExecutionDiagnosticListener).Assembly.GetName().Name!;

    private static string GetVersion()
        => typeof(ActivityExecutionDiagnosticListener).Assembly.GetName().Version!.ToString();
}
