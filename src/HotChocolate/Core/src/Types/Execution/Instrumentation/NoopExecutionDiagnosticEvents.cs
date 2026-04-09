namespace HotChocolate.Execution.Instrumentation;

internal sealed class NoopExecutionDiagnosticEvents
    : ExecutionDiagnosticEventListener
{
    private NoopExecutionDiagnosticEvents()
    {
    }

    public static NoopExecutionDiagnosticEvents Instance { get; } = new();
}
