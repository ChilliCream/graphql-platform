using System.Diagnostics;

namespace Mocha.Mediator;

internal static class MochaMediatorActivitySource
{
    public static ActivitySource Source { get; } = new(GetName(), GetVersion());

    public static string GetName()
        => typeof(ActivityMediatorDiagnosticListener).Assembly.GetName().Name!;

    private static string GetVersion()
        => typeof(ActivityMediatorDiagnosticListener).Assembly.GetName().Version!.ToString();
}
