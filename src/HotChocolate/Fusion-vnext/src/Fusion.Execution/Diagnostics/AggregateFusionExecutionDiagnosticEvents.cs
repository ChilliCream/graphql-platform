using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Diagnostics;

internal sealed class AggregateFusionExecutionDiagnosticEvents : IFusionExecutionDiagnosticEvents
{
    private readonly IFusionExecutionDiagnosticEventListener[] _listeners;

    public AggregateFusionExecutionDiagnosticEvents(IFusionExecutionDiagnosticEventListener[] listeners)
    {
        _listeners = listeners;
    }

    public IDisposable ExecuteRequest(RequestContext context)
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].ExecuteRequest(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ParseDocument(RequestContext context)
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].ParseDocument(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ValidateDocument(RequestContext context)
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].ValidateDocument(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable CoerceVariables(RequestContext context)
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].CoerceVariables(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ExecuteOperation(RequestContext context)
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].ExecuteOperation(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ExecuteSubscription(RequestContext context)
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].ExecuteSubscription(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable OnSubscriptionEvent(RequestContext context)
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].OnSubscriptionEvent(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public void ExecutionError(RequestContext context, ErrorKind kind, IReadOnlyList<IError> errors, object? state = null)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].ExecutionError(context, kind, errors, state);
        }
    }

    public void AddedDocumentToCache(RequestContext context)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].AddedDocumentToCache(context);
        }
    }

    public void RetrievedDocumentFromCache(RequestContext context)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].RetrievedDocumentFromCache(context);
        }
    }

    public void RetrievedDocumentFromStorage(RequestContext context)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].RetrievedDocumentFromStorage(context);
        }
    }

    public void DocumentNotFoundInStorage(RequestContext context, OperationDocumentId documentId)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].DocumentNotFoundInStorage(context, documentId);
        }
    }

    public void ExecutorCreated(string name, IRequestExecutor executor)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].ExecutorCreated(name, executor);
        }
    }

    public void ExecutorEvicted(string name, IRequestExecutor executor)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].ExecutorEvicted(name, executor);
        }
    }

    private sealed class AggregateActivityScope : IDisposable
    {
        private readonly IDisposable[] _scopes;
        private bool _disposed;

        public AggregateActivityScope(IDisposable[] scopes)
        {
            _scopes = scopes;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                for (var i = 0; i < _scopes.Length; i++)
                {
                    _scopes[i].Dispose();
                }
                _disposed = true;
            }
        }
    }
}
