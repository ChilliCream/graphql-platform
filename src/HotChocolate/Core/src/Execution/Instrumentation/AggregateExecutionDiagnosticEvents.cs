using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation;

internal sealed class AggregateExecutionDiagnosticEvents : IExecutionDiagnosticEvents
{
    private readonly IExecutionDiagnosticEventListener[] _listeners;
    private readonly IExecutionDiagnosticEventListener[] _resolverListener;

    public AggregateExecutionDiagnosticEvents(IExecutionDiagnosticEventListener[] listeners)
    {
        _listeners = listeners;
        _resolverListener = listeners.Where(t => t.EnableResolveFieldValue).ToArray();
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

    public void RequestError(RequestContext context, Exception error)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].RequestError(context, error);
        }
    }

    public void RequestError(RequestContext context, IError error)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].RequestError(context, error);
        }
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

    public void ValidationErrors(RequestContext context, IReadOnlyList<IError> errors)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].ValidationErrors(context, errors);
        }
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

    public void UntrustedDocumentRejected(RequestContext context)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].UntrustedDocumentRejected(context);
        }
    }

    public IDisposable AnalyzeOperationCost(RequestContext context)
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].AnalyzeOperationCost(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public void OperationCost(RequestContext context, double fieldCost, double typeCost)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].OperationCost(context, fieldCost, typeCost);
        }
    }

    public IDisposable CompileOperation(RequestContext context)
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].CompileOperation(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public void AddedOperationToCache(RequestContext context)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].AddedOperationToCache(context);
        }
    }

    public void RetrievedOperationFromCache(RequestContext context)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].RetrievedOperationFromCache(context);
        }
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

    public IDisposable ExecuteStream(IOperation operation)
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].ExecuteStream(operation);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ExecuteDeferredTask()
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].ExecuteDeferredTask();
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ResolveFieldValue(IMiddlewareContext context)
    {
        if (_resolverListener.Length == 0)
        {
            return ExecutionDiagnosticEventListener.EmptyScope;
        }

        var scopes = new IDisposable[_resolverListener.Length];

        for (var i = 0; i < _resolverListener.Length; i++)
        {
            scopes[i] = _resolverListener[i].ResolveFieldValue(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public void ResolverError(IMiddlewareContext context, IError error)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].ResolverError(context, error);
        }
    }

    public void ResolverError(RequestContext context, ISelection selection, IError error)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].ResolverError(context, selection, error);
        }
    }

    public IDisposable RunTask(IExecutionTask task)
    {
        if (_resolverListener.Length == 0)
        {
            return ExecutionDiagnosticEventListener.EmptyScope;
        }

        var scopes = new IDisposable[_resolverListener.Length];

        for (var i = 0; i < _resolverListener.Length; i++)
        {
            scopes[i] = _resolverListener[i].RunTask(task);
        }

        return new AggregateActivityScope(scopes);
    }

    public void TaskError(IExecutionTask task, IError error)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].TaskError(task, error);
        }
    }

    public void StartProcessing(RequestContext context)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].StartProcessing(context);
        }
    }

    public void StopProcessing(RequestContext context)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].StopProcessing(context);
        }
    }

    public IDisposable ExecuteSubscription(RequestContext context, ulong subscriptionId)
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].ExecuteSubscription(context, subscriptionId);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable OnSubscriptionEvent(RequestContext context, ulong subscriptionId)
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].OnSubscriptionEvent(context, subscriptionId);
        }

        return new AggregateActivityScope(scopes);
    }

    public void SubscriptionEventError(RequestContext context, ulong subscriptionId, Exception exception)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].SubscriptionEventError(context, subscriptionId, exception);
        }
    }

    public IDisposable DispatchBatch(RequestContext context)
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].DispatchBatch(context);
        }

        return new AggregateActivityScope(scopes);
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

    private sealed class AggregateActivityScope(IDisposable[] scopes) : IDisposable
    {
        private readonly IDisposable[] _scopes = scopes;
        private bool _disposed;

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
