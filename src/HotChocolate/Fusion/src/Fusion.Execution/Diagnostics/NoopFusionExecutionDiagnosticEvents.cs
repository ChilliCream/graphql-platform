using HotChocolate.Execution.Instrumentation;

namespace HotChocolate.Fusion.Diagnostics;

internal sealed class NoopFusionExecutionDiagnosticEvents
    : FusionExecutionDiagnosticEventListener
    , INoopExecutionDiagnosticEvents
{
    private NoopFusionExecutionDiagnosticEvents()
    {
    }

    public static NoopFusionExecutionDiagnosticEvents Instance { get; } = new();
}
