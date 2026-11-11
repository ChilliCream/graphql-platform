using System.Collections.Concurrent;
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
        var duration = timeout ?? _defaultTimeout;
        var timeoutSource = new CancellationTokenSource(duration, timeProvider);
        return AddPromise(correlationId, duration, timeoutSource.Token, timeoutSource);
    }

    internal CancellationTokenSource CreateRequestTimeout() => new(_defaultTimeout, timeProvider);

    internal TaskCompletionSource<object?> AddPromise(string correlationId, CancellationToken timeoutToken)
        => AddPromise(correlationId, _defaultTimeout, timeoutToken, ownedTimeout: null);

    private TaskCompletionSource<object?> AddPromise(
        string correlationId,
        TimeSpan timeout,
        CancellationToken timeoutToken,
        CancellationTokenSource? ownedTimeout)
    {
        var tcs = new TaskCompletionSource<object?>(RunContinuationsAsynchronously);
        var promise = new Promise(tcs, ownedTimeout, timeout);
        _matches.TryAdd(correlationId, promise);

        timeoutToken.Register(() =>
        {
            if (_matches.TryRemove(correlationId, out var p))
            {
                p.TaskCompletionSource.TrySetException(ThrowHelper.ResponseTimedOut(correlationId, p.Timeout));
                p.OwnedTimeout?.Dispose();
            }
        });

        return tcs;
    }

    internal async ValueTask ReleasePromiseAsync(string correlationId, Task completion)
    {
        if (_matches.TryRemove(correlationId, out var promise))
        {
            promise.OwnedTimeout?.Dispose();
            promise.TaskCompletionSource.TrySetCanceled();
        }

        try
        {
            await completion;
        }
        catch
        {
            // The request has already reported its response or dispatch failure.
        }
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
            promise.OwnedTimeout?.Dispose();
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
            promise.OwnedTimeout?.Dispose();
            promise.TaskCompletionSource.SetResult(response);
            return true;
        }

        return false;
    }

    private sealed record Promise(
        TaskCompletionSource<object?> TaskCompletionSource,
        CancellationTokenSource? OwnedTimeout,
        TimeSpan Timeout);
}
