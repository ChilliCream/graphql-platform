using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Diagnostic;

internal sealed class NoopFusionDiagnosticEvents : IFusionDiagnosticEvents, IDisposable
{
    public IDisposable ExecuteFederatedQuery(IRequestContext context)
        => this;

    public void QueryPlanExecutionError(Exception exception)
    {
    }

    public void ResolveError(Exception exception)
    {
    }

    public void ResolveByKeyBatchError(Exception exception)
    {
    }

    public void Dispose()
    {
    }
}
