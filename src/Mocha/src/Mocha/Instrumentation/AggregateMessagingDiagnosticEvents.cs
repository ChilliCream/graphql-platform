using System.Collections.Immutable;
using Mocha.Middlewares;

namespace Mocha;

internal sealed class AggregateMessagingDiagnosticEvents : IMessagingDiagnosticEvents
{
    private ImmutableArray<IMessagingDiagnosticEventListener> _listeners;

    public AggregateMessagingDiagnosticEvents(IMessagingDiagnosticEventListener[] listeners)
    {
        _listeners = [.. listeners];
    }

    public IDisposable Subscribe(IMessagingDiagnosticEventListener listener)
    {
        ImmutableInterlocked.Update(ref _listeners, static (list, l) => list.Add(l), listener);
        return new Subscription(this, listener);
    }

    private void Unsubscribe(IMessagingDiagnosticEventListener listener)
    {
        ImmutableInterlocked.Update(ref _listeners, static (list, l) => list.Remove(l), listener);
    }

    public IDisposable Dispatch(IDispatchContext context)
    {
        var listeners = _listeners;
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].Dispatch(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public void DispatchError(IDispatchContext context, Exception exception)
    {
        var listeners = _listeners;

        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].DispatchError(context, exception);
        }
    }

    public IDisposable Receive(IReceiveContext context)
    {
        var listeners = _listeners;
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].Receive(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public void ReceiveError(IReceiveContext context, Exception exception)
    {
        var listeners = _listeners;

        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].ReceiveError(context, exception);
        }
    }

    public IDisposable Consume(IConsumeContext context)
    {
        var listeners = _listeners;
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].Consume(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public void ConsumeError(IConsumeContext context, Exception exception)
    {
        var listeners = _listeners;

        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].ConsumeError(context, exception);
        }
    }

    private sealed class Subscription(
        AggregateMessagingDiagnosticEvents aggregate,
        IMessagingDiagnosticEventListener listener) : IDisposable
    {
        public void Dispose() => aggregate.Unsubscribe(listener);
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
