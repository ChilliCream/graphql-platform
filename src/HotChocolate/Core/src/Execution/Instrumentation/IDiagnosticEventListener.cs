namespace HotChocolate.Execution.Instrumentation
{
    public interface IDiagnosticEventListener
        : IDiagnosticEvents
    {
        bool EnableResolveFieldValue { get; }
    }
}
