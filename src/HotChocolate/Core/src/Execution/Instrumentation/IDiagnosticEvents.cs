namespace HotChocolate.Execution.Instrumentation
{
    public interface IDiagnosticEvents
    {
        IActivityScope ParseDocument(IRequestContext context);
    }

}
