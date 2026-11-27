using System.Diagnostics;

namespace Mocha.Mediator;

internal static class MochaMediatorActivitySource
{
    public static ActivitySource Source { get; } = new(MochaTelemetryNames.Mediator, GetVersion());

    private static string GetVersion()
        => typeof(ActivityMediatorDiagnosticListener).Assembly.GetName().Version!.ToString();
}
