using System.Collections.Concurrent;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationCacheMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private readonly IPreparedOperationCache _operationCache;
    private readonly ConcurrentDictionary<string, Lazy<TaskCompletionSource<Operation>>> _inFlightOperations =
        new(StringComparer.Ordinal);

    private OperationCacheMiddleware(
        RequestDelegate next,
        [SchemaService] IExecutionDiagnosticEvents diagnosticEvents,
        [SchemaService] IPreparedOperationCache operationCache)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);
        ArgumentNullException.ThrowIfNull(operationCache);

        _next = next;
        _diagnosticEvents = diagnosticEvents;
        _operationCache = operationCache;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        var documentId = context.GetOperationDocumentId();

        if (documentId.IsEmpty)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (!context.TryGetOperationId(out var operationId))
        {
            operationId = context.CreateCacheId();
            context.SetOperationId(operationId);
        }

        var isSingleFlightLeader = false;
        Lazy<TaskCompletionSource<Operation>>? inFlightOperation = null;

        if (_operationCache.TryGetOperation(operationId, out var operation))
        {
            context.SetOperation(operation);
            _diagnosticEvents.RetrievedOperationFromCache(context);
        }
        else if (_inFlightOperations.TryGetValue(operationId, out inFlightOperation))
        {
            // Another request is already compiling this operation.
            // Await the leader's result to avoid redundant compilation work.
            var coalescedOperation = await inFlightOperation.Value.Task
                .WaitAsync(context.RequestAborted)
                .ConfigureAwait(false);
            context.SetOperation(coalescedOperation);
        }
        else
        {
            // No operation is cached and no compilation is in progress.
            // Use a Lazy<TCS> so that under burst conditions only one TCS is materialized
            // even if multiple requests race through GetOrAdd concurrently.
            inFlightOperation = new Lazy<TaskCompletionSource<Operation>>(
                static () => new TaskCompletionSource<Operation>(
                    TaskCreationOptions.RunContinuationsAsynchronously));
            var cachedInFlightOperation = _inFlightOperations.GetOrAdd(operationId, inFlightOperation);

            if (ReferenceEquals(cachedInFlightOperation, inFlightOperation))
            {
                // We won the race! This request is the single-flight leader
                // responsible for compiling and signaling all followers.
                isSingleFlightLeader = true;
                context.Features.Set(inFlightOperation.Value);
            }
            else
            {
                // We lost the race! Another request claimed leadership between
                // TryGetValue and GetOrAdd. So we simply await the leader's result.
                var coalescedOperation = await cachedInFlightOperation.Value.Task
                    .WaitAsync(context.RequestAborted)
                    .ConfigureAwait(false);
                context.SetOperation(coalescedOperation);
            }
        }

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Propagate the exception to all waiting followers.
            if (isSingleFlightLeader && inFlightOperation is not null)
            {
                inFlightOperation.Value.TrySetException(ex);
            }

            throw;
        }
        finally
        {
            if (isSingleFlightLeader)
            {
                // Guard against a faulty diagnostic event handler preventing cleanup.
                // Without this, a throw from the cache or diagnostics would leak the
                // in-flight entry, causing _inFlightOperations to grow indefinitely.
                try
                {
                    if (context.TryGetOperation(out operation))
                    {
                        // Cache the operation before removing the in-flight entry so that
                        // there is no window where the operation is in neither structure.
                        _operationCache.TryAddOperation(operation.Id, operation);
                        _diagnosticEvents.AddedOperationToCache(context);
                        inFlightOperation?.Value.TrySetResult(operation);
                    }
                    else if (inFlightOperation?.Value.Task.IsCompleted == false)
                    {
                        // The pipeline completed without producing an operation and without
                        // throwing. Signal followers so they do not hang indefinitely.
                        inFlightOperation.Value.TrySetException(
                            new InvalidOperationException(
                                "The operation compilation task completed without a result."));
                    }
                }
                finally
                {
                    _inFlightOperations.TryRemove(operationId, out _);
                }
            }
        }
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();
                var cache = core.SchemaServices.GetRequiredService<IPreparedOperationCache>();
                var middleware = new OperationCacheMiddleware(next, diagnosticEvents, cache);
                return context => middleware.InvokeAsync(context);
            },
            WellKnownRequestMiddleware.OperationCacheMiddleware);
}
