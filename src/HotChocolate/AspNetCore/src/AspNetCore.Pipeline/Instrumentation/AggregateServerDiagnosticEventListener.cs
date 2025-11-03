using Microsoft.AspNetCore.Http;
using HotChocolate.Language;

namespace HotChocolate.AspNetCore.Instrumentation;

internal sealed class AggregateServerDiagnosticEventListener : IServerDiagnosticEventListener
{
    private readonly IServerDiagnosticEventListener[] _listeners;

    public AggregateServerDiagnosticEventListener(IServerDiagnosticEventListener[] listeners)
    {
        _listeners = listeners ?? throw new ArgumentNullException(nameof(listeners));
    }

    public IDisposable ExecuteHttpRequest(HttpContext context, HttpRequestKind kind)
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].ExecuteHttpRequest(context, kind);
        }

        return new AggregateActivityScope(scopes);
    }

    public void StartSingleRequest(HttpContext context, GraphQLRequest request)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].StartSingleRequest(context, request);
        }
    }

    public void StartBatchRequest(HttpContext context, IReadOnlyList<GraphQLRequest> batch)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].StartBatchRequest(context, batch);
        }
    }

    public void StartOperationBatchRequest(
        HttpContext context,
        GraphQLRequest request,
        IReadOnlyList<string> operations)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].StartOperationBatchRequest(context, request, operations);
        }
    }

    public void HttpRequestError(HttpContext context, IError error)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].HttpRequestError(context, error);
        }
    }

    public void HttpRequestError(HttpContext context, Exception exception)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].HttpRequestError(context, exception);
        }
    }

    public IDisposable ParseHttpRequest(HttpContext context)
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].ParseHttpRequest(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public void ParserErrors(HttpContext context, IReadOnlyList<IError> errors)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].ParserErrors(context, errors);
        }
    }

    public IDisposable FormatHttpResponse(HttpContext context, IOperationResult result)
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].FormatHttpResponse(context, result);
        }

        return new AggregateActivityScope(scopes);
    }

    public IDisposable WebSocketSession(HttpContext context)
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].WebSocketSession(context);
        }

        return new AggregateActivityScope(scopes);
    }

    public void WebSocketSessionError(HttpContext context, Exception exception)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].WebSocketSessionError(context, exception);
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
