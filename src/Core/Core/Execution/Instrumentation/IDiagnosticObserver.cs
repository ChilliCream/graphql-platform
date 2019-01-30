namespace HotChocolate.Execution.Instrumentation
{
    public interface IDiagnosticObserver
    {
        bool IsEnabled(string name, object payload, object context);
    }
}
