namespace HotChocolate.Fusion.Diagnostics;

internal sealed class NoopFusionExecutionDiagnosticEvents
    : FusionExecutionDiagnosticEventListener
{
    private NoopFusionExecutionDiagnosticEvents()
    {
    }

    public static NoopFusionExecutionDiagnosticEvents Instance { get; } = new();
}
