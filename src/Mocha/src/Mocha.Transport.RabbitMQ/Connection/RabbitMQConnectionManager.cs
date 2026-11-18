using Mocha.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Owns one RabbitMQ connection: connects it at start, hands it out while it is ready, and replaces it in
/// the background after it was lost.
/// </summary>
/// <param name="logger">The logger for connection lifecycle events.</param>
/// <param name="timeProvider">The clock used for elapsed times and retry delays.</param>
/// <param name="options">The timeouts and the retry delay of the connection.</param>
/// <param name="connectionFactory">Creates a new RabbitMQ connection.</param>
/// <param name="setUpConnectionAsync">
/// Prepares a new connection before it is handed out. A failure discards the connection and counts as a
/// failed attempt.
/// </param>
internal sealed class RabbitMQConnectionManager(
    RabbitMQConnectionLogger logger,
    TimeProvider timeProvider,
    RabbitMQConnectionManagerOptions options,
    Func<CancellationToken, ValueTask<IConnection>> connectionFactory,
    Func<IConnection, CancellationToken, Task> setUpConnectionAsync) : IAsyncDisposable
{
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly object _sync = new();

    // The ready connection, or null while there is none.
    private IConnection? _connection;

    // Completes with the next ready connection, and is replaced when that connection is lost.
    private TaskCompletionSource<IConnection> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // The connect routine that runs or ran last. At most one runs at a time.
    private Task _connecting = Task.CompletedTask;

    // Completed while the ready connection is not blocked by the broker, and replaced while it is.
    private TaskCompletionSource _unblocked = TaskCompletionSource.CreateCompleted();

    private Task? _disposeTask;
    private long _unavailableSince;
    private long _blockedSince;
    private bool _isDisposed;

    /// <summary>
    /// Gets the ready connection, or <c>null</c> while there is none.
    /// </summary>
    public IConnection? Connection => Volatile.Read(ref _connection) is { IsOpen: true } connection ? connection : null;

    /// <summary>
    /// Gets whether a ready connection exists.
    /// </summary>
    public bool IsConnected => Connection is not null;

    /// <summary>
    /// Gets whether the broker blocked the ready connection. The broker does not read from a blocked
    /// connection, so publishes and requests on it stay unanswered until the broker unblocks it.
    /// </summary>
    public bool IsBlocked => Volatile.Read(ref _blockedSince) != 0 && IsConnected;

    /// <summary>
    /// Waits while the broker blocks the ready connection. The wait also ends when the connection is lost.
    /// </summary>
    public Task WaitWhileBlockedAsync(CancellationToken cancellationToken)
    {
        var unblocked = Volatile.Read(ref _unblocked).Task;
        return unblocked.IsCompleted ? Task.CompletedTask : unblocked.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Gets whether the manager was disposed.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref _isDisposed);

    /// <summary>
    /// Gets the ready connection, waiting while there is none, without connecting: only
    /// <see cref="StartAsync"/> and the loss of a connection connect. The wait ends when a connection becomes
    /// ready, when <paramref name="cancellationToken"/> is cancelled, or when the manager is disposed.
    /// </summary>
    public ValueTask<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
        => Volatile.Read(ref _connection) is { IsOpen: true } connection
            ? new ValueTask<IConnection>(connection)
            : WaitForConnectionAsync(cancellationToken);

    private async ValueTask<IConnection> WaitForConnectionAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            Task<IConnection>? ready = null;
            IConnection? closed = null;
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_isDisposed, this);
                if (_connection is { } current)
                {
                    if (current.IsOpen)
                    {
                        return current;
                    }

                    closed = current;
                }
                else
                {
                    // The wait does not connect. Before a start succeeded it lasts until one does.
                    ready = _ready.Task;
                }
            }

            if (closed is not null)
            {
                // The connection closed and its shutdown event has not arrived yet.
                OnConnectionLost(closed, closed.GetCloseReason());
                continue;
            }

            var connection = await ready!.WaitAsync(cancellationToken);
            if (connection.IsOpen)
            {
                return connection;
            }
        }
    }

    /// <summary>
    /// Connects, retrying with exponential backoff until the start timeout elapsed. A failure that repeats on
    /// every attempt, such as refused credentials, fails at once.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            Task? connecting = null;
            Task<IConnection>? ready = null;
            IConnection? closed = null;
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_isDisposed, this);
                if (_connection is { } current)
                {
                    if (current.IsOpen)
                    {
                        return;
                    }

                    closed = current;
                }
                else if (_connecting.IsCompleted)
                {
                    var timeout = options.StartTimeout;
                    _unavailableSince = timeProvider.GetTimestamp();
                    _connecting = connecting = Task.Run(() => ConnectWithRetryAsync(timeout, cancellationToken));
                }
                else
                {
                    ready = _ready.Task;
                }
            }

            if (closed is not null)
            {
                OnConnectionLost(closed, closed.GetCloseReason());
                continue;
            }

            if (connecting is not null)
            {
                try
                {
                    await connecting;
                    return;
                }
                catch (OperationCanceledException)
                    when (_disposeCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    throw ThrowHelper.ConnectionManagerDisposed();
                }
            }

            // A recovery already runs in the background. The start waits for it as long as it would have
            // kept trying itself, which is without a limit for an infinite timeout.
            var startTimeout = options.StartTimeout;
            try
            {
                await ready!.WaitAsync(startTimeout, timeProvider, cancellationToken);
                return;
            }
            catch (TimeoutException ex)
            {
                throw ThrowHelper.ConnectionTimedOut(startTimeout, ex);
            }
        }
    }

    /// <summary>
    /// Connects until an attempt succeeds. With a <paramref name="startTimeout"/> the routine fails when that
    /// time elapsed or a failure repeats on every attempt, and with <c>null</c>, for a recovery, it never
    /// gives up.
    /// </summary>
    private async Task ConnectWithRetryAsync(TimeSpan? startTimeout, CancellationToken cancellationToken)
    {
        using var loopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCts.Token);
        var loopToken = loopCts.Token;
        var started = timeProvider.GetTimestamp();

        for (var attempt = 1; ; attempt++)
        {
            loopToken.ThrowIfCancellationRequested();

            // The last attempt before the start timeout ends with it. A timeout of zero allows one whole
            // attempt, and an infinite one, which is negative, sets no end.
            var attemptTimeout = options.AttemptTimeout;
            if (startTimeout is { } limit && limit > TimeSpan.Zero)
            {
                var remaining = limit - timeProvider.GetElapsedTime(started);
                if (remaining < attemptTimeout)
                {
                    attemptTimeout = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
                }
            }

            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(loopToken);
            attemptCts.CancelAfter(attemptTimeout);

            try
            {
                logger.ConnectionAttemptStarted(attempt);

                var connection = await ConnectOnceAsync(attemptCts.Token);
                if (!TryHandOut(connection, attempt))
                {
                    // The manager was disposed while the attempt ran. Disposal cancelled the routine's token
                    // before it set the flag, so this ends the routine.
                    await CloseConnectionAsync(connection);
                    loopToken.ThrowIfCancellationRequested();
                }

                return;
            }
            catch (Exception ex) when (!loopToken.IsCancellationRequested)
            {
                // Nobody cancelled the routine, so a cancellation is a timeout: of the attempt, or of the
                // client, which reports a request the broker did not answer in time as a cancellation.
                var timedOut = ex is OperationCanceledException;
                var failure =
                    !timedOut ? ex
                    : attemptCts.IsCancellationRequested ? ThrowHelper.ConnectionAttemptTimedOut(attemptTimeout, ex)
                    : ThrowHelper.ConnectionRequestTimedOut(ex);
                var delay = ExponentialBackoff.CalculateDelay(
                    attempt,
                    options.InitialRetryDelay,
                    options.MaxRetryDelay);
                var elapsed = timeProvider.GetElapsedTime(Volatile.Read(ref _unavailableSince));

                // A start gives up when the failure repeats on every attempt, or when the next attempt would
                // begin after a timeout that is not infinite.
                if (startTimeout is { } timeout
                    && (
                        failure.IsPermanentFailure()
                        || (
                            timeout != Timeout.InfiniteTimeSpan
                            && timeProvider.GetElapsedTime(started) + delay > timeout)))
                {
                    logger.ConnectionFailed(failure, attempt, elapsed);

                    if (timedOut)
                    {
                        throw ThrowHelper.ConnectionTimedOut(timeout, ex);
                    }

                    throw;
                }

                logger.ConnectionAttemptFailed(failure, attempt, elapsed, delay);

                await Task.Delay(delay, timeProvider, loopToken);
            }
        }
    }

    private async Task<IConnection> ConnectOnceAsync(CancellationToken cancellationToken)
    {
        var connection = await connectionFactory(cancellationToken);
        try
        {
            connection.ConnectionShutdownAsync += (_, reason) =>
            {
                OnConnectionLost(connection, reason);
                return Task.CompletedTask;
            };
            connection.CallbackExceptionAsync += OnCallbackExceptionAsync;
            connection.ConnectionBlockedAsync += (_, e) =>
            {
                OnConnectionBlocked(connection, e.Reason);
                return Task.CompletedTask;
            };
            connection.ConnectionUnblockedAsync += (_, _) =>
            {
                OnConnectionUnblocked(connection);
                return Task.CompletedTask;
            };

            // The client does not end a request with the token once the request is sent.
            await setUpConnectionAsync(connection, cancellationToken).WaitOrAbandonAsync(cancellationToken);

            if (!connection.IsOpen)
            {
                throw ThrowHelper.ConnectionClosedDuringSetup(connection.CloseReason);
            }

            return connection;
        }
        catch
        {
            await CloseConnectionAsync(connection);
            throw;
        }
    }

    private bool TryHandOut(IConnection connection, int attempt)
    {
        lock (_sync)
        {
            if (_isDisposed)
            {
                return false;
            }

            Volatile.Write(ref _blockedSince, 0);
            _unblocked.TrySetResult();
            Volatile.Write(ref _connection, connection);
            _ready.TrySetResult(connection);
        }

        logger.ConnectionReady(attempt, timeProvider.GetElapsedTime(Volatile.Read(ref _unavailableSince)));

        // A connection that closed before it was handed out raised its shutdown event to nobody.
        if (!connection.IsOpen)
        {
            OnConnectionLost(connection, connection.GetCloseReason());
        }

        return true;
    }

    private void OnConnectionLost(IConnection connection, ShutdownEventArgs? reason)
    {
        if (!TryBeginRecovery(connection))
        {
            return;
        }

        logger.ConnectionLost(reason);
    }

    /// <summary>
    /// Starts replacing the ready connection in the background. Returns <c>false</c> when
    /// <paramref name="connection"/> is not the ready connection, so only the first report for a connection
    /// has an effect.
    /// </summary>
    private bool TryBeginRecovery(IConnection connection)
    {
        lock (_sync)
        {
            if (_isDisposed || !ReferenceEquals(_connection, connection))
            {
                return false;
            }

            // Callers that arrive from here on wait for the next connection.
            _ready = new TaskCompletionSource<IConnection>(TaskCreationOptions.RunContinuationsAsynchronously);
            Volatile.Write(ref _connection, null);
            Volatile.Write(ref _unavailableSince, timeProvider.GetTimestamp());
            Volatile.Write(ref _blockedSince, 0);
            _unblocked.TrySetResult();

            // The client raises the shutdown event on its reader loop, which must not wait for the cleanup.
            _connecting = Task.Run(() => RecoverAsync(connection));
            return true;
        }
    }

    private async Task RecoverAsync(IConnection lostConnection)
    {
        // The lost connection is closed first, so the broker has released what belongs to it, such as an
        // exclusive queue. Disposing it also stops the client's own automatic recovery.
        await CloseConnectionAsync(lostConnection);
        await ConnectWithRetryAsync(startTimeout: null, CancellationToken.None);
    }

    private Task OnCallbackExceptionAsync(object? sender, CallbackExceptionEventArgs e)
    {
        logger.ConnectionCallbackFailed(e.Exception, e.Detail);
        return Task.CompletedTask;
    }

    private void OnConnectionBlocked(IConnection connection, string reason)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(_connection, connection))
            {
                return;
            }

            if (_blockedSince == 0)
            {
                Volatile.Write(ref _blockedSince, timeProvider.GetTimestamp());
                Volatile.Write(
                    ref _unblocked,
                    new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
            }
        }

        logger.ConnectionBlocked(reason);
    }

    private void OnConnectionUnblocked(IConnection connection)
    {
        long blockedSince;
        lock (_sync)
        {
            if (!ReferenceEquals(_connection, connection))
            {
                return;
            }

            blockedSince = _blockedSince;
            Volatile.Write(ref _blockedSince, 0);
            _unblocked.TrySetResult();
        }

        logger.ConnectionUnblocked(blockedSince == 0 ? TimeSpan.Zero : timeProvider.GetElapsedTime(blockedSince));
    }

    /// <summary>
    /// Opens a channel on a connection. The open does not end with a caller's token, because the client keeps
    /// the channel number of a cancelled open until the connection closes. When the broker does not answer
    /// within <paramref name="timeout"/>, the connection is closed, which starts a recovery.
    /// </summary>
    /// <exception cref="ChannelOpenBlockedException">
    /// The broker did not answer while it blocked the connection. The connection stays, and the channel
    /// number of the open is lost until the connection is replaced.
    /// </exception>
    public async Task<IChannel> OpenChannelAsync(IConnection connection, CreateChannelOptions options, TimeSpan timeout)
    {
        using var openCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token);
        openCts.CancelAfter(timeout);

        try
        {
            // The client does not end the request with the token once the request is sent.
            return await connection.CreateChannelAsync(options, openCts.Token).WaitOrAbandonAsync(openCts.Token);
        }
        catch (Exception) when (_disposeCts.IsCancellationRequested)
        {
            throw ThrowHelper.ConnectionManagerDisposed();
        }
        catch (OperationCanceledException ex)
        {
            // The timeout elapsed, or the client's own timeout for a request did.
            lock (_sync)
            {
                if (_blockedSince != 0 && ReferenceEquals(_connection, connection))
                {
                    // The broker does not read from a blocked connection, so the silence says nothing about
                    // the connection. Replacing it would send every pending publish again.
                    throw ThrowHelper.ChannelOpenBlocked(ex);
                }
            }

            // Closing the connection is the only way to give the channel number back, and it ends a request
            // that is still in flight.
            ReportUnresponsive(connection, "the broker did not answer the request to open a channel");
            throw ThrowHelper.ChannelOpenTimedOut(ex);
        }
    }

    /// <summary>
    /// Replaces the ready connection because the broker stopped answering on it. The connection is closed in
    /// the background before the next one is created.
    /// </summary>
    /// <param name="connection">The connection the broker does not answer on.</param>
    /// <param name="reason">Why the connection is replaced, for the log.</param>
    public void ReportUnresponsive(IConnection connection, string reason)
    {
        if (!TryBeginRecovery(connection))
        {
            return;
        }

        logger.ConnectionAborted(reason);
    }

    /// <summary>
    /// Closes and disposes a connection, waiting no longer than the close timeout for either step.
    /// </summary>
    private async Task CloseConnectionAsync(IConnection connection)
    {
        try
        {
            if (connection.IsOpen)
            {
                await connection.AbortAsync(options.CloseTimeout);
            }
        }
        catch (Exception ex)
        {
            logger.ConnectionCloseFailed(ex);
        }

        try
        {
            // Disposing a connection whose broker stopped answering can take as long as the client's own timeouts.
            await connection.DisposeAsync().AsTask().WaitAsync(options.CloseTimeout);
        }
        catch (Exception ex)
        {
            logger.ConnectionCloseFailed(ex);
        }
    }

    /// <summary>
    /// Closes and disposes a channel, ignoring failures of either step and waiting no longer than the close
    /// timeout. A close that takes longer continues in the background.
    /// </summary>
    public async Task CloseChannelAsync(IChannel channel)
    {
        var closing = channel.CloseCompletelyAsync();
        if (closing.IsCompleted)
        {
            return;
        }

        try
        {
            await closing.WaitAsync(options.CloseTimeout);
        }
        catch (TimeoutException)
        {
            // The broker does not answer.
        }
    }

    /// <summary>
    /// Stops connecting and closes the connection. Pending and later calls of
    /// <see cref="GetConnectionAsync"/> fail with <see cref="ObjectDisposedException"/>.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            return new ValueTask(_disposeTask ??= DisposeCoreAsync());
        }
    }

    private async Task DisposeCoreAsync()
    {
        // Yields first, so the caller's lock is not held while the cancellation callbacks run.
        await Task.Yield();
        await _disposeCts.CancelAsync();

        IConnection? connection;
        Task connecting;
        lock (_sync)
        {
            _isDisposed = true;
            connection = _connection;
            _connection = null;
            connecting = _connecting;

            _unblocked.TrySetResult();
            _ready.TrySetException(ThrowHelper.ConnectionManagerDisposed());

            // Reading the exception marks it as observed when nobody waits for a connection.
            _ = _ready.Task.Exception;
        }

        try
        {
            await connecting;
        }
        catch
        {
            // The connect routine ended with the cancellation above, or failed the start it belonged to.
        }

        if (connection is not null)
        {
            await CloseConnectionAsync(connection);
        }

        logger.ConnectionStopped();
    }
}

file static class Extensions
{
    extension(TaskCompletionSource)
    {
        public static TaskCompletionSource CreateCompleted()
        {
            var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            source.SetResult();
            return source;
        }
    }

    extension(IConnection connection)
    {
        /// <summary>
        /// Gets the close reason of the connection, or <c>null</c> when the connection is disposed.
        /// </summary>
        public ShutdownEventArgs? GetCloseReason()
        {
            try
            {
                return connection.CloseReason;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }
    }
}
