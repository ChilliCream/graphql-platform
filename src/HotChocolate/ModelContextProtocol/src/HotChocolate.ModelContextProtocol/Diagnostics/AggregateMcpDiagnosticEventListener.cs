namespace HotChocolate.ModelContextProtocol.Diagnostics;

internal sealed class AggregateMcpDiagnosticEvents(IMcpDiagnosticEventListener[] listeners)
    : IMcpDiagnosticEvents
{
    public IDisposable InitializeTools()
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].InitializeTools();
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable UpdateTools()
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].InitializeTools();
        }

        return new AggregateActivityScope(scopes);
    }

    public void ValidationErrors(IReadOnlyList<IError> errors)
    {
        foreach (var listener in listeners)
        {
            listener.ValidationErrors(errors);
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
