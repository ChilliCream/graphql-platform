namespace Mocha.Mediator;

internal sealed class AggregateMediatorDiagnosticEvents(IMediatorDiagnosticEventListener[] listeners)
    : IMediatorDiagnosticEvents
{
    public IDisposable Execute(Type messageType, Type responseType, object message)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].Execute(messageType, responseType, message);
        }

        return new AggregateActivityScope(scopes);
    }

    public void ExecutionError(Type messageType, Type responseType, object message, Exception exception)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].ExecutionError(messageType, responseType, message, exception);
        }
    }

    private sealed class AggregateActivityScope(IDisposable[] scopes) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                for (var i = 0; i < scopes.Length; i++)
                {
                    scopes[i].Dispose();
                }

                _disposed = true;
            }
        }
    }
}
