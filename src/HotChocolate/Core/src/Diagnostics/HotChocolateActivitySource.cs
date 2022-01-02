using System.Diagnostics;

namespace HotChocolate.Diagnostics;

internal static class HotChocolateActivitySource
{
    public static ActivitySource Source { get; } = new(GetName(), GetVersion());

    private static string GetName()
        => typeof(ActivityExecutionDiagnosticListener).Assembly.GetName().Name!;

    private static string GetVersion()
        => typeof(ActivityExecutionDiagnosticListener).Assembly.GetName().Version!.ToString();
}
