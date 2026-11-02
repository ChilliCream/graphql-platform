using Mocha.Middlewares;

namespace Mocha;

internal sealed class AggregateMessagingDiagnosticEvents(IMessagingDiagnosticEventListener[] listeners)
    : IMessagingDiagnosticEvents
{
    public IDisposable Dispatch(IDispatchContext context)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].Dispatch(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public void DispatchError(IDispatchContext context, Exception exception)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].DispatchError(context, exception);
        }
    }

    public IDisposable Receive(IReceiveContext context)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].Receive(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public void ReceiveError(IReceiveContext context, Exception exception)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].ReceiveError(context, exception);
        }
    }

    public IDisposable Consume(IConsumeContext context)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].Consume(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public void ConsumeError(IConsumeContext context, Exception exception)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].ConsumeError(context, exception);
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
