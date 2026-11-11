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
internal sealed partial class RabbitMQConnectionManager : IAsyncDisposable
{
    private static readonly TimeSpan s_initialRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan s_maxRetryDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan s_closeTimeout = TimeSpan.FromSeconds(5);
    private static readonly CancellationToken s_cancelled = new(canceled: true);

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
    /// <param name="connectionName">The name that identifies the connection in logs.</param>
    /// <param name="timeProvider">The clock used for elapsed times and retry delays.</param>
    /// <param name="connectionFactory">Creates a new RabbitMQ connection.</param>
    /// <param name="setUpConnectionAsync">
    /// Prepares a new connection before it is handed out. A failure discards the connection and counts as a
    /// failed attempt.
    /// </param>
    public RabbitMQConnectionManager(
        ILogger logger,
        string connectionName,
        TimeProvider timeProvider,
        Func<CancellationToken, ValueTask<IConnection>> connectionFactory,
        Func<IConnection, CancellationToken, Task> setUpConnectionAsync)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
        _setUpConnectionAsync = setUpConnectionAsync;
        ConnectionName = connectionName;
        TimeProvider = timeProvider;
    }

    /// <summary>
    /// Gets the name that identifies the connection in logs.
    /// </summary>
    public string ConnectionName { get; }

    /// <summary>
    /// Gets the clock used for elapsed times and retry delays.
    /// </summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>
    /// Gets or sets how long <see cref="StartAsync"/> keeps retrying while the broker cannot be reached.
    /// </summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromMinutes(2);

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
    /// Gets the ready connection, waiting while there is none. The wait ends when a connection becomes
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
            Task? connecting = null;
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
                    if (_connecting.IsCompleted)
                    {
                        // The manager was not started, or its start failed.
                        _unavailableSince = TimeProvider.GetTimestamp();
                        _connecting = Task.Run(() => ConnectWithRetryAsync(deadline: null, CancellationToken.None));
                    }

                    ready = _ready.Task;
                    connecting = _connecting;
                }
            }

            if (closed is not null)
            {
                // The connection closed and its shutdown event has not arrived yet.
                OnConnectionLost(closed, GetCloseReason(closed));
                continue;
            }

            // A start that fails ends its routine without a connection. The wait then starts the next one.
            await Task.WhenAny(ready!, connecting!).WaitAsync(cancellationToken);
            if (ready!.IsCompleted)
            {
                var connection = await ready;
                if (connection.IsOpen)
                {
                    return connection;
                }
            }
        }
    }

    /// <summary>
    /// Connects, retrying with exponential backoff until <see cref="ConnectTimeout"/> elapsed. A failure that
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
                    var timeout = ConnectTimeout;
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
            // kept trying itself.
            try
            {
                await ready!.WaitAsync(ConnectTimeout, TimeProvider, cancellationToken);
                return;
            }
            catch (TimeoutException ex)
            {
                throw ThrowHelper.ConnectionTimedOut(ConnectTimeout, ex);
            }
        }
    }

    private async Task ConnectWithRetryAsync(TimeSpan? deadline, CancellationToken cancellationToken)
    {
        using var loopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCts.Token);
        var loopToken = loopCts.Token;
        var started = TimeProvider.GetTimestamp();

        for (var attempt = 1; ; attempt++)
        {
            loopToken.ThrowIfCancellationRequested();

            // The last attempt before the deadline ends with the deadline.
            var attemptTimeout = AttemptTimeout;
            if (deadline is { } limit && limit > TimeSpan.Zero)
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
                    _logger.ConnectionAttemptStarted(ConnectionName, attempt);
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

                if (deadline is { } connectTimeout
                    && (RabbitMQFailure.IsPermanent(failure)
                        || TimeProvider.GetElapsedTime(started) + delay > connectTimeout))
                {
                    using (BeginLogScope())
                    {
                        _logger.ConnectionFailed(failure, ConnectionName, attempt, elapsed);
                    }

                    if (timedOut)
                    {
                        throw ThrowHelper.ConnectionTimedOut(connectTimeout, ex);
                    }

                    throw;
                }

                using (BeginLogScope())
                {
                    _logger.ConnectionAttemptFailed(
                        failure,
                        RabbitMQFailure.GetLogLevel(failure),
                        ConnectionName,
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
                ConnectionName,
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

        using (BeginLogScope())
        {
            _logger.ConnectionLost(
                ConnectionName,
                reason?.ReplyCode ?? 0,
                reason?.ReplyText,
                reason?.Initiator ?? ShutdownInitiator.Library,
                reason?.Exception?.Message);
        }
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
        await ConnectWithRetryAsync(deadline: null, CancellationToken.None);
    }

    private Task OnCallbackExceptionAsync(object? sender, CallbackExceptionEventArgs e)
    {
        using (BeginLogScope())
        {
            _logger.ConnectionCallbackFailed(e.Exception, ConnectionName, Print(e.Detail));
        }

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

        using (BeginLogScope())
        {
            _logger.ConnectionBlocked(ConnectionName, reason);
        }
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

        using (BeginLogScope())
        {
            _logger.ConnectionUnblocked(
                ConnectionName,
                blockedSince == 0 ? TimeSpan.Zero : TimeProvider.GetElapsedTime(blockedSince));
        }
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
    /// The broker did not answer because it blocked the connection. The connection stays, and the channel
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

        using (BeginLogScope())
        {
            _logger.ConnectionAborted(ConnectionName, reason);
        }
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
                await connection.AbortAsync(s_closeTimeout);
            }
        }
        catch (Exception ex)
        {
            LogCloseFailed(ex);
        }

        try
        {
            // Disposing a connection whose broker stopped answering can take as long as the client's own timeouts.
            await connection.DisposeAsync().AsTask().WaitAsync(s_closeTimeout);
        }
        catch (Exception ex)
        {
            LogCloseFailed(ex);
        }
    }

    private void LogCloseFailed(Exception exception)
    {
        using (BeginLogScope())
        {
            _logger.ConnectionCloseFailed(exception, ConnectionName);
        }
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
            await closing.WaitAsync(s_closeTimeout);
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

        using (BeginLogScope())
        {
            _logger.ConnectionStopped(ConnectionName);
        }
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
