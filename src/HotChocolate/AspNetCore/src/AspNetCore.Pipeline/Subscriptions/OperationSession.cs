using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.AspNetCore.Subscriptions;

internal sealed class OperationSession : IOperationSession
{
    private readonly CancellationTokenSource _cts = new();
    private readonly CancellationToken _ct;
    private readonly ISocketSession _session;
    private readonly ISocketSessionInterceptor _interceptor;
    private readonly ExecutorSession _executorSession;
    private bool _disposed;

    public event EventHandler? Completed;

    public OperationSession(
        ISocketSession session,
        ExecutorSession executorSession,
        string id)
    {
        _session = session;
        _executorSession = executorSession;
        _interceptor = executorSession.SocketSessionInterceptor;
        _ct = _cts.Token;
        Id = id;
    }

    public string Id { get; }

    public bool IsCompleted { get; private set; }

    public void BeginExecute(GraphQLRequest request, CancellationToken cancellationToken)
        => SendResultsAsync(request, cancellationToken).FireAndForget();

    private async Task SendResultsAsync(GraphQLRequest request, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _ct);
        var ct = cts.Token;
        var completeTry = false;

        try
        {
            var requestBuilder = CreateRequestBuilder(request);
            await _interceptor.OnRequestAsync(_session, Id, requestBuilder, ct);
            await using var result = await _executorSession.ExecuteAsync(requestBuilder.Build(), ct);

            switch (result)
            {
                case IOperationResult queryResult:
                    if (queryResult.Data is null && queryResult.Errors is { Count: > 0 })
                    {
                        await _session.Protocol.SendErrorMessageAsync(
                            _session,
                            Id,
                            queryResult.Errors,
                            ct);
                    }
                    else
                    {
                        await SendResultMessageAsync(queryResult, ct);
                    }
                    break;

                case IResponseStream responseStream:
                    await foreach (var item in responseStream.ReadResultsAsync().WithCancellation(ct))
                    {
                        try
                        {
                            // use the original cancellation token here to keep the websocket open for other streams.
                            await SendResultMessageAsync(item, cancellationToken);
                        }
                        finally
                        {
                            await item.DisposeAsync();
                        }
                    }
                    break;
            }

            // The operation is completed, and we will try to send a complete message.
            // We mark 'completeTry' true so that in case of an error, we do not try to send this
            // message again.
            completeTry = true;

            if (!ct.IsCancellationRequested)
            {
                await _session.Protocol.SendCompleteMessageAsync(_session, Id, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // the operation was cancelled so we do nothings
        }
        catch (Exception ex)
        {
            // if the error happened while the operation was not yet complete we will try
            // to send an error message and complete the subscription.
            if (!completeTry)
            {
                await TrySendErrorMessageAsync(ex, ct);
            }
        }
        finally
        {
            try
            {
                // we use the original cancellation token which represents the request cancellation to
                // invoke OnCompleteAsync this allows for an easy extension point to get rid of
                // any resources that might be bound to the subscription.
                await _interceptor.OnCompleteAsync(_session, Id, cancellationToken);
            }
            catch
            {
                // we will just ignore any user exceptions here so we can graciously close
                // the subscription out.
            }

            // signal that the subscription is completed.
            Complete();
        }
    }

    private static OperationRequestBuilder CreateRequestBuilder(GraphQLRequest request)
    {
        var requestBuilder = new OperationRequestBuilder();

        if (request.Document is not null)
        {
            requestBuilder.SetDocument(request.Document);
        }

        if (request.OperationName is not null)
        {
            requestBuilder.SetOperationName(request.OperationName);
        }

        if (request.DocumentId is not null)
        {
            requestBuilder.SetDocumentId(request.DocumentId);
        }

        if (request.DocumentHash is not null)
        {
            requestBuilder.SetDocumentHash(request.DocumentHash);
        }

        if (request.Variables is not null)
        {
            requestBuilder.SetVariableValuesSet(request.Variables);
        }

        if (request.Extensions is not null)
        {
            requestBuilder.SetExtensions(request.Extensions);
        }

        return requestBuilder;
    }

    private async Task SendResultMessageAsync(IOperationResult result, CancellationToken ct)
    {
        result = await _interceptor.OnResultAsync(_session, Id, result, ct);
        await _session.Protocol.SendResultMessageAsync(_session, Id, result, ct);
    }

    private async Task TrySendErrorMessageAsync(Exception exception, CancellationToken ct)
    {
        try
        {
            if (!ct.IsCancellationRequested)
            {
                var error = ErrorBuilder.FromException(exception).Build();
                error = _executorSession.Handle(error);

                var errors =
                    error is AggregateError aggregateError
                        ? aggregateError.Errors
                        : [error];

                await _session.Protocol.SendErrorMessageAsync(_session, Id, errors, ct);
            }
        }
        catch
        {
            // if we cannot send the complete message we just go on. This mostly will happen
            // if the client is already disconnected or the operation was cancelled.
        }
    }

    private void Complete()
    {
        try
        {
            IsCompleted = true;
            Completed?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // we ignore any error that might happen on invoking complete.
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cts.Cancel();
            _cts.Dispose();
            _disposed = true;
        }
    }
}
