using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Diagnostics;

internal sealed class AggregateFusionExecutionDiagnosticEvents(
    IFusionExecutionDiagnosticEventListener[] listeners)
    : IFusionExecutionDiagnosticEvents
{
    public IDisposable ExecuteRequest(RequestContext context)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteRequest(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ParseDocument(RequestContext context)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ParseDocument(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ValidateDocument(RequestContext context)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ValidateDocument(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable CoerceVariables(RequestContext context)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].CoerceVariables(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ExecuteOperation(RequestContext context)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteOperation(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ExecuteSubscription(RequestContext context, ulong subscriptionId)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteSubscription(context, subscriptionId);
        }

        return new AggregateActivityScope(scopes);
    }

    public void ExecutionError(
        RequestContext context,
        ErrorKind kind,
        IReadOnlyList<IError> errors,
        object? state = null)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].ExecutionError(context, kind, errors, state);
        }
    }

    public void AddedDocumentToCache(RequestContext context)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].AddedDocumentToCache(context);
        }
    }

    public void RetrievedDocumentFromCache(RequestContext context)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].RetrievedDocumentFromCache(context);
        }
    }

    public void RetrievedDocumentFromStorage(RequestContext context)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].RetrievedDocumentFromStorage(context);
        }
    }

    public void DocumentNotFoundInStorage(RequestContext context, OperationDocumentId documentId)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].DocumentNotFoundInStorage(context, documentId);
        }
    }

    public IDisposable PlanOperation(RequestContext context)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].PlanOperation(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ExecuteOperationNode(OperationPlanContext context, OperationExecutionNode node)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteOperationNode(context, node);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ExecuteSubscriptionNode(
        OperationPlanContext context,
        OperationExecutionNode node,
        ulong subscriptionId)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteSubscriptionNode(context, node, subscriptionId);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable ExecuteIntrospectionNode(OperationPlanContext context, IntrospectionExecutionNode node)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteIntrospectionNode(context, node);
        }

        return new AggregateActivityScope(scopes);
    }

    public void ExecutorCreated(string name, IRequestExecutor executor)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].ExecutorCreated(name, executor);
        }
    }

    public void ExecutorEvicted(string name, IRequestExecutor executor)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].ExecutorEvicted(name, executor);
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
