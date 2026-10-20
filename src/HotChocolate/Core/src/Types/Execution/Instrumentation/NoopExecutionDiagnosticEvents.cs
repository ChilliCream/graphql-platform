namespace HotChocolate.Execution.Instrumentation;

internal sealed class NoopExecutionDiagnosticEvents
    : ExecutionDiagnosticEventListener
    , INoopExecutionDiagnosticEvents
{
    private NoopExecutionDiagnosticEvents()
    {
    }

    public static NoopExecutionDiagnosticEvents Instance { get; } = new();
}
