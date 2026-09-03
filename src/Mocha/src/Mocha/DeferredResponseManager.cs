using System.Collections.Concurrent;
using Mocha.Events;
using static System.Threading.Tasks.TaskCreationOptions;

namespace Mocha;

/// <summary>
/// Manages deferred request-response correlations by tracking outstanding promises keyed by correlation identifier and completing them when responses arrive or timeouts expire.
/// </summary>
public sealed class DeferredResponseManager(TimeProvider timeProvider)
{
    private readonly ConcurrentDictionary<string, Promise> _matches = new();
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Registers a new deferred response promise for the specified correlation identifier with an optional timeout.
    /// </summary>
    /// <param name="correlationId">The correlation identifier used to match the incoming response to this promise.</param>
    /// <param name="timeout">The duration after which the promise expires. Defaults to 2 minutes if not specified.</param>
    /// <returns>A <see cref="TaskCompletionSource{T}"/> that completes when the correlated response arrives or the timeout elapses.</returns>
    public TaskCompletionSource<object?> AddPromise(string correlationId, TimeSpan? timeout = null)
    {
        timeout ??= _defaultTimeout;
        var tcs = new TaskCompletionSource<object?>(RunContinuationsAsynchronously);
        var cts = new CancellationTokenSource(timeout.Value, timeProvider);
        var promise = new Promise(tcs, cts, timeout.Value);

        cts.Token.Register(() =>
        {
            if (_matches.TryRemove(correlationId, out var p))
            {
                p.TaskCompletionSource.TrySetException(new ResponseTimeoutException(correlationId, p.Timeout));
            }
        });

        _matches.TryAdd(correlationId, promise);
        return tcs;
    }

    /// <summary>
    /// Awaits and returns the result of a previously registered promise.
    /// </summary>
    /// <param name="correlationId">The correlation identifier of the promise to await.</param>
    /// <returns>The response object, or <c>null</c> if the response had no payload.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no promise exists for the given correlation identifier.</exception>
    public async Task<object?> GetPromise(string correlationId)
    {
        if (_matches.TryGetValue(correlationId, out var promise))
        {
            return await promise.TaskCompletionSource.Task;
        }

        throw ThrowHelper.PromiseNotFound();
    }

    /// <summary>
    /// Faults the promise associated with the specified correlation identifier by setting an exception.
    /// </summary>
    /// <param name="correlationId">The correlation identifier of the promise to fault.</param>
    /// <param name="exception">The exception to propagate to the waiting caller.</param>
    public void SetException(string correlationId, Exception exception)
    {
        if (_matches.TryRemove(correlationId, out var promise))
        {
            promise.Cts.Cancel();
            promise.Cts.Dispose();
            promise.TaskCompletionSource.SetException(exception);
        }
    }

    /// <summary>
    /// Completes the promise for the specified correlation identifier with the given response payload.
    /// </summary>
    /// <param name="correlationId">The correlation identifier of the promise to complete.</param>
    /// <param name="response">The response object to deliver to the waiting caller.</param>
    /// <returns><c>true</c> if a matching promise was found and completed; <c>false</c> if no promise was registered for the correlation identifier.</returns>
    public bool CompletePromise(string correlationId, object? response)
    {
        if (_matches.TryRemove(correlationId, out var promise))
        {
            promise.Cts.Cancel();
            promise.Cts.Dispose();
            promise.TaskCompletionSource.SetResult(response);
            return true;
        }

        return false;
    }

    private sealed record Promise(
        TaskCompletionSource<object?> TaskCompletionSource,
        CancellationTokenSource Cts,
        TimeSpan Timeout);
}
