using System.Text;
using Microsoft.Extensions.Logging;
using Mocha.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Owns one RabbitMQ connection: connects it at start, hands it out while it is ready, and replaces it in
/// the background after it was lost.
/// </summary>
internal sealed class RabbitMQConnectionManager : IAsyncDisposable
{
    private static readonly TimeSpan s_initialRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan s_maxRetryDelay = TimeSpan.FromSeconds(10);
    private static readonly CancellationToken s_cancelled = new(canceled: true);

    /// <summary>
    /// The longest wait for the broker while a connection, a channel or a temporary queue is released.
    /// </summary>
    public static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(5);

    private readonly ILogger _logger;
    private readonly Func<CancellationToken, ValueTask<IConnection>> _connectionFactory;
    private readonly Func<IConnection, CancellationToken, Task> _setUpConnectionAsync;
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly object _sync = new();

    // The ready connection, or null while there is none.
    private IConnection? _connection;

    // Completes with the next ready connection, and is replaced when that connection is lost.
    private TaskCompletionSource<IConnection> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // The connect routine that runs or ran last. At most one runs at a time.
    private Task _connecting = Task.CompletedTask;
    // Completed while the ready connection is not blocked by the broker, and replaced while it is.
    private TaskCompletionSource _unblocked = CreateCompletedSource();

    private Task? _disposeTask;
    private long _unavailableSince;
    private long _blockedSince;
    private bool _isDisposed;

    /// <summary>
    /// Creates a connection manager.
    /// </summary>
    /// <param name="logger">The logger for connection lifecycle events.</param>
    /// <param name="connectionRole">The role of the connection, which names it in logs.</param>
    /// <param name="timeProvider">The clock used for elapsed times and retry delays.</param>
    /// <param name="connectionFactory">Creates a new RabbitMQ connection.</param>
    /// <param name="setUpConnectionAsync">
    /// Prepares a new connection before it is handed out. A failure discards the connection and counts as a
    /// failed attempt.
    /// </param>
    public RabbitMQConnectionManager(
        ILogger logger,
        string connectionRole,
        TimeProvider timeProvider,
        Func<CancellationToken, ValueTask<IConnection>> connectionFactory,
        Func<IConnection, CancellationToken, Task> setUpConnectionAsync)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
        _setUpConnectionAsync = setUpConnectionAsync;
        ConnectionRole = connectionRole;
        TimeProvider = timeProvider;
    }

    /// <summary>
    /// Gets the role of the connection, which names it in logs.
    /// </summary>
    public string ConnectionRole { get; }

    /// <summary>
    /// Gets the clock used for elapsed times and retry delays.
    /// </summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>
    /// Gets the properties that identify the connection in structured logs: service, instance, transport,
    /// connection name and role, broker host, port and virtual host.
    /// </summary>
    public IReadOnlyDictionary<string, object?> LogContext { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// Begins a logger scope that carries <see cref="LogContext"/>.
    /// </summary>
    public IDisposable? BeginLogScope() => _logger.BeginScope(LogContext);

    /// <summary>
    /// Gets or sets how long <see cref="StartAsync"/> keeps retrying while the broker cannot be reached.
    /// <see cref="Timeout.InfiniteTimeSpan"/> keeps retrying until the caller cancels.
    /// </summary>
    public TimeSpan StartTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets or sets how long one connection attempt may take before it is abandoned and retried.
    /// </summary>
    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(30);

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
                OnConnectionLost(closed, GetCloseReason(closed));
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
    /// Connects, retrying with exponential backoff until <see cref="StartTimeout"/> elapsed. A failure that
    /// repeats on every attempt, such as refused credentials, fails at once.
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
                    var timeout = StartTimeout;
                    _unavailableSince = TimeProvider.GetTimestamp();
                    _connecting = connecting = Task.Run(() => ConnectWithRetryAsync(timeout, cancellationToken));
                }
                else
                {
                    ready = _ready.Task;
                }
            }

            if (closed is not null)
            {
                OnConnectionLost(closed, GetCloseReason(closed));
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
            var startTimeout = StartTimeout;
            try
            {
                await ready!.WaitAsync(startTimeout, TimeProvider, cancellationToken);
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
        var started = TimeProvider.GetTimestamp();

        // Every log call below has its own scope: a scope around the routine would be captured by the reader
        // loop of the connection that is created inside it.
        for (var attempt = 1; ; attempt++)
        {
            loopToken.ThrowIfCancellationRequested();

            // The last attempt before the start timeout ends with it. A timeout of zero allows one whole
            // attempt, and an infinite one, which is negative, sets no end.
            var attemptTimeout = AttemptTimeout;
            if (startTimeout is { } limit && limit > TimeSpan.Zero)
            {
                var remaining = limit - TimeProvider.GetElapsedTime(started);
                if (remaining < attemptTimeout)
                {
                    attemptTimeout = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
                }
            }

            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(loopToken);
            attemptCts.CancelAfter(attemptTimeout);

            try
            {
                using (BeginLogScope())
                {
                    _logger.ConnectionAttemptStarted(ConnectionRole, attempt);
                }

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
                var failure = !timedOut ? ex
                    : attemptCts.IsCancellationRequested ? ThrowHelper.ConnectionAttemptTimedOut(attemptTimeout, ex)
                    : ThrowHelper.ConnectionRequestTimedOut(ex);
                var delay = ExponentialBackoff.CalculateDelay(attempt, s_initialRetryDelay, s_maxRetryDelay);
                var elapsed = TimeProvider.GetElapsedTime(Volatile.Read(ref _unavailableSince));

                // A start gives up when the failure repeats on every attempt, or when the next attempt would
                // begin after a timeout that is not infinite.
                if (startTimeout is { } timeout
                    && (RabbitMQFailure.IsPermanent(failure)
                        || (timeout != Timeout.InfiniteTimeSpan
                            && TimeProvider.GetElapsedTime(started) + delay > timeout)))
                {
                    using (BeginLogScope())
                    {
                        _logger.ConnectionFailed(failure, ConnectionRole, attempt, elapsed);
                    }

                    if (timedOut)
                    {
                        throw ThrowHelper.ConnectionTimedOut(timeout, ex);
                    }

                    throw;
                }

                using (BeginLogScope())
                {
                    _logger.ConnectionAttemptFailed(
                        failure,
                        RabbitMQFailure.GetLogLevel(failure),
                        ConnectionRole,
                        attempt,
                        elapsed,
                        delay);
                }

                await Task.Delay(delay, TimeProvider, loopToken);
            }
        }
    }

    private async Task<IConnection> ConnectOnceAsync(CancellationToken cancellationToken)
    {
        var connection = await _connectionFactory(cancellationToken);
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
            await _setUpConnectionAsync(connection, cancellationToken).WaitOrAbandonAsync(cancellationToken);

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

        using (BeginLogScope())
        {
            _logger.ConnectionReady(
                ConnectionRole,
                attempt,
                TimeProvider.GetElapsedTime(Volatile.Read(ref _unavailableSince)));
        }

        // A connection that closed before it was handed out raised its shutdown event to nobody.
        if (!connection.IsOpen)
        {
            OnConnectionLost(connection, GetCloseReason(connection));
        }

        return true;
    }

    private void OnConnectionLost(IConnection connection, ShutdownEventArgs? reason)
    {
        if (!TryBeginRecovery(connection))
        {
            return;
        }

        // The client repeats the reply text as the cause when the socket failed.
        var cause = reason?.Exception?.Message;
        using var _ = BeginLogScope();
        _logger.ConnectionLost(
            ConnectionRole,
            reason?.ReplyCode ?? 0,
            reason?.ReplyText,
            reason?.Initiator ?? ShutdownInitiator.Library,
            cause is null || cause == reason?.ReplyText ? "none" : cause);
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
            Volatile.Write(ref _unavailableSince, TimeProvider.GetTimestamp());
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
        using var _ = BeginLogScope();
        _logger.ConnectionCallbackFailed(e.Exception, ConnectionRole, Print(e.Detail));
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
                Volatile.Write(ref _blockedSince, TimeProvider.GetTimestamp());
                Volatile.Write(ref _unblocked, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
            }
        }

        using var _ = BeginLogScope();
        _logger.ConnectionBlocked(ConnectionRole, reason);
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

        using var _ = BeginLogScope();
        _logger.ConnectionUnblocked(
            ConnectionRole,
            blockedSince == 0 ? TimeSpan.Zero : TimeProvider.GetElapsedTime(blockedSince));
    }

    // A disposed connection of the client throws instead of giving its close reason.
    private static ShutdownEventArgs? GetCloseReason(IConnection connection)
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

    private static TaskCompletionSource CreateCompletedSource()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        source.SetResult();
        return source;
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
    public async Task<IChannel> OpenChannelAsync(
        IConnection connection,
        CreateChannelOptions options,
        TimeSpan timeout)
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

        using var _ = BeginLogScope();
        _logger.ConnectionAborted(ConnectionRole, reason);
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
                await connection.AbortAsync(CloseTimeout);
            }
        }
        catch (Exception ex)
        {
            LogCloseFailed(ex);
        }

        try
        {
            // Disposing a connection whose broker stopped answering can take as long as the client's own timeouts.
            await connection.DisposeAsync().AsTask().WaitAsync(CloseTimeout);
        }
        catch (Exception ex)
        {
            LogCloseFailed(ex);
        }
    }

    private void LogCloseFailed(Exception exception)
    {
        using var _ = BeginLogScope();
        _logger.ConnectionCloseFailed(exception, ConnectionRole);
    }

    /// <summary>
    /// Closes and disposes a channel, ignoring failures of either step and waiting no longer than the close
    /// timeout. A close that takes longer continues in the background.
    /// </summary>
    public static async Task CloseChannelAsync(IChannel channel)
    {
        var closing = CloseChannelCompletelyAsync(channel);
        if (closing.IsCompleted)
        {
            return;
        }

        try
        {
            await closing.WaitAsync(CloseTimeout);
        }
        catch (TimeoutException)
        {
            // The broker does not answer.
        }
    }

    /// <summary>
    /// Closes and disposes a channel, ignoring failures of either step. Completes when the client has given
    /// the channel number back, which takes as long as the client's timeout for a request when the broker
    /// does not answer.
    /// </summary>
    public static async Task CloseChannelCompletelyAsync(IChannel channel)
    {
        try
        {
            if (channel.IsOpen)
            {
                await channel.CloseAsync();
            }
            else
            {
                // A connection with automatic recovery forgets a channel the broker closed only when the
                // channel is closed here as well. The cancelled token keeps the client from waiting for a reply.
                await channel.CloseAsync(Constants.ReplySuccess, "Goodbye", abort: true, s_cancelled);
            }
        }
        catch
        {
            // Ignore close errors.
        }
        finally
        {
            try
            {
                await channel.DisposeAsync();
            }
            catch
            {
                // Ignore disposal errors.
            }
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

        // The discard name is taken by the read of the exception above.
        using var scope = BeginLogScope();
        _logger.ConnectionStopped(ConnectionRole);
    }

    private static string Print(IDictionary<string, object>? detail)
    {
        if (detail is null || detail.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var item in detail)
        {
            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(item.Key).Append(": ").Append(item.Value);
        }

        return builder.ToString();
    }
}
