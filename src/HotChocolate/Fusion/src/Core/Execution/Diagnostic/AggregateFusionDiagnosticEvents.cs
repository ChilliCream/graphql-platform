using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Diagnostic;

internal sealed class AggregateFusionDiagnosticEvents(IFusionDiagnosticEventListener[] listeners)
    : IFusionDiagnosticEventListener
{
    public IDisposable ExecuteFederatedQuery(IRequestContext context)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteFederatedQuery(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public void QueryPlanExecutionError(Exception exception)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].QueryPlanExecutionError(exception);
        }
    }

    public void ResolveError(Exception exception)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].ResolveError(exception);
        }
    }

    public void ResolveByKeyBatchError(Exception exception)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].ResolveByKeyBatchError(exception);
        }
    }

    public void SubgraphRequestError(string subgraphName, Exception exception)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].SubgraphRequestError(subgraphName, exception);
        }
    }

    private sealed class AggregateActivityScope(IDisposable[] scopes) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var scope in scopes)
                {
                    scope.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
