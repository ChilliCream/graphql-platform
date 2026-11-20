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
    private readonly RabbitMQConnectionLogger _logger = logger;
    private readonly RabbitMQConnectionManagerOptions _options = options;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly object _sync = new();

    /// <summary>
    /// The state of the manager. Every transition replaces it with one atomic exchange.
    /// </summary>
    private State _state = new NotStarted(new(TaskCreationOptions.RunContinuationsAsynchronously));

    private Task? _disposeTask;

    /// <summary>
    /// Gets the ready connection, or <c>null</c> while there is none.
    /// </summary>
    public IConnection? Connection
        => Volatile.Read(ref _state).Connection is { IsOpen: true } connection ? connection : null;

    /// <summary>
    /// Gets whether a ready connection exists.
    /// </summary>
    public bool IsConnected => Connection is not null;

    /// <summary>
    /// Gets whether the broker blocked the ready connection. The broker does not read from a blocked
    /// connection, so publishes and requests on it stay unanswered until the broker unblocks it.
    /// </summary>
    public bool IsBlocked => Volatile.Read(ref _state).IsBlocked;

    /// <summary>
    /// Waits while the broker blocks the ready connection. The wait also ends when the connection is lost.
    /// </summary>
    public Task WaitWhileBlockedAsync(CancellationToken cancellationToken)
        => Volatile.Read(ref _state).WaitWhileBlockedAsync(cancellationToken);

    /// <summary>
    /// Gets whether the manager was disposed.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref _state) is Disposed;

    /// <summary>
    /// Gets the ready connection, waiting while there is none, without connecting: only
    /// <see cref="StartAsync"/> and the loss of a connection connect. The wait ends when a connection becomes
    /// ready, when <paramref name="cancellationToken"/> is cancelled, or when the manager is disposed.
    /// </summary>
    public ValueTask<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
        => Volatile.Read(ref _state).Connection is { IsOpen: true } connection
            ? new ValueTask<IConnection>(connection)
            : WaitForConnectionAsync(cancellationToken);

    private async ValueTask<IConnection> WaitForConnectionAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            // A state that was left while the caller waited gives no connection, and the next state is asked.
            if (await Volatile.Read(ref _state).GetConnectionAsync(this, cancellationToken) is { } connection)
            {
                return connection;
            }
        }
    }

    /// <summary>
    /// Connects, retrying with exponential backoff until the start timeout elapsed. A failure that repeats on
    /// every attempt, such as refused credentials, fails at once. Concurrent callers share one start, and
    /// <paramref name="cancellationToken"/> ends only the wait of its caller.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (!await Volatile.Read(ref _state).StartAsync(this, cancellationToken))
        {
            // The state was left while the caller waited, so the next state is asked.
        }
    }

    /// <summary>
    /// Replaces the state <paramref name="from"/> with <paramref name="to"/>. Returns <c>false</c> when another
    /// transition replaced <paramref name="from"/> first.
    /// </summary>
    private bool TryEnter(State from, State to) => Interlocked.CompareExchange(ref _state, to, from) == from;

    private async Task<(IConnection Connection, Alarm Alarm)> ConnectOnceAsync(CancellationToken cancellationToken)
    {
        var connection = await connectionFactory(cancellationToken);
        var alarm = new Alarm();
        try
        {
            connection.ConnectionShutdownAsync += (_, reason) =>
            {
                OnConnectionLost(connection, reason);
                return Task.CompletedTask;
            };
            connection.CallbackExceptionAsync += (_, e) =>
            {
                _logger.ConnectionCallbackFailed(e.Exception, e.Detail);
                return Task.CompletedTask;
            };
            connection.ConnectionBlockedAsync += (_, e) =>
            {
                alarm.Begin(_timeProvider.GetTimestamp());
                _logger.ConnectionBlocked(e.Reason);
                return Task.CompletedTask;
            };
            connection.ConnectionUnblockedAsync += (_, _) =>
            {
                var blockedSince = alarm.End();
                _logger.ConnectionUnblocked(
                    blockedSince is { } since ? _timeProvider.GetElapsedTime(since) : TimeSpan.Zero);
                return Task.CompletedTask;
            };

            // The client does not end a request with the token once the request is sent.
            await setUpConnectionAsync(connection, cancellationToken).WaitOrAbandonAsync(cancellationToken);

            if (!connection.IsOpen)
            {
                throw ThrowHelper.ConnectionClosedDuringSetup(connection.CloseReason);
            }

            return (connection, alarm);
        }
        catch
        {
            await CloseConnectionAsync(connection);
            throw;
        }
    }

    private void OnConnectionLost(IConnection connection, ShutdownEventArgs? reason)
    {
        if (TryBeginRecovery(connection))
        {
            _logger.ConnectionLost(reason);
        }
    }

    /// <summary>
    /// Starts replacing the ready connection in the background. Returns <c>false</c> when
    /// <paramref name="connection"/> is not the ready connection, so only the first report for a connection
    /// has an effect.
    /// </summary>
    private bool TryBeginRecovery(IConnection connection)
    {
        // Several reports can arrive for one connection, also at the same time. A report that arrives after
        // the state was replaced ends here.
        if (Volatile.Read(ref _state) is not Connected connected
            || !ReferenceEquals(connected.Connection, connection))
        {
            return false;
        }

        // Callers that arrive from here on wait for the next connection.
        var recovering = new Recovering(connection, _timeProvider.GetTimestamp());
        if (!TryEnter(connected, recovering))
        {
            // A concurrent report for the connection, or a disposal, replaced the state first.
            return false;
        }

        // The lost connection is no longer blocked, and the alarm ignores a late block event from it.
        connected.Alarm.End(retire: true);

        // The routine is not awaited, because the caller can be the reader loop of the client.
        _ = recovering.Run(this);
        return true;
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
            if (Volatile.Read(ref _state) is Connected { Alarm.IsActive: true } connected
                && ReferenceEquals(connected.Connection, connection))
            {
                // The broker does not read from a blocked connection, so the silence says nothing about
                // the connection. Replacing it would send every pending publish again.
                throw ThrowHelper.ChannelOpenBlocked(ex);
            }

            // The client keeps the channel number of this open reserved until the connection closes. The
            // connection is replaced, so the numbers do not run out.
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
        if (TryBeginRecovery(connection))
        {
            _logger.ConnectionAborted(reason);
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
                await connection.AbortAsync(_options.CloseTimeout);
            }
        }
        catch (Exception ex)
        {
            _logger.ConnectionCloseFailed(ex);
        }

        try
        {
            // Disposing a connection whose broker stopped answering can take as long as the client's own timeouts.
            await connection.DisposeAsync().AsTask().WaitAsync(_options.CloseTimeout);
        }
        catch (Exception ex)
        {
            _logger.ConnectionCloseFailed(ex);
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
            await closing.WaitAsync(_options.CloseTimeout);
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

        var last = Interlocked.Exchange(ref _state, new Disposed());
        await last.DisposeAsync(this);

        _logger.ConnectionStopped();
    }

    /// <summary>
    /// One state of the manager. The members hold what the manager does while it is in the state.
    /// </summary>
    private abstract class State
    {
        /// <summary>
        /// Gets the connection of the state, or <c>null</c> when the state has none.
        /// </summary>
        public virtual IConnection? Connection => null;

        /// <summary>
        /// Gets whether the broker blocks the connection of the state.
        /// </summary>
        public virtual bool IsBlocked => false;

        /// <summary>
        /// Waits while the broker blocks the connection of the state.
        /// </summary>
        public virtual Task WaitWhileBlockedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Starts the manager. Returns <c>false</c> when the state was left and the next state must be asked.
        /// </summary>
        public abstract ValueTask<bool> StartAsync(
            RabbitMQConnectionManager manager,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the ready connection. Returns <c>null</c> when the state was left and the next state must be
        /// asked.
        /// </summary>
        public abstract ValueTask<IConnection?> GetConnectionAsync(
            RabbitMQConnectionManager manager,
            CancellationToken cancellationToken);

        /// <summary>
        /// Releases what the state holds, after the manager left it for the disposed state.
        /// </summary>
        public abstract Task DisposeAsync(RabbitMQConnectionManager manager);
    }

    /// <summary>
    /// A state without a connection. Callers that ask for a connection wait for <see cref="Ready"/>.
    /// </summary>
    private abstract class Waiting(TaskCompletionSource<IConnection> ready) : State
    {
        /// <summary>
        /// Completes with the next ready connection.
        /// </summary>
        public TaskCompletionSource<IConnection> Ready { get; } = ready;

        public override async ValueTask<IConnection?> GetConnectionAsync(
            RabbitMQConnectionManager manager,
            CancellationToken cancellationToken)
        {
            await Ready.Task.WaitAsync(cancellationToken);

            // The state was left when the connection became ready, and the next state owns the connection.
            return null;
        }

        public override Task DisposeAsync(RabbitMQConnectionManager manager)
        {
            Ready.TrySetException(ThrowHelper.ConnectionManagerDisposed());

            // Reading the exception marks it as observed when nobody waits for a connection.
            _ = Ready.Task.Exception;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// No connection exists and nothing connects: before the first start, and after a start that failed.
    /// </summary>
    private sealed class NotStarted(TaskCompletionSource<IConnection> ready) : Waiting(ready)
    {
        public override ValueTask<bool> StartAsync(
            RabbitMQConnectionManager manager,
            CancellationToken cancellationToken)
        {
            // The caller that begins the start waits for that start, so it gets the failure of that start.
            var starting = new Starting(Ready, manager._timeProvider.GetTimestamp());
            return manager.TryEnter(this, starting)
                ? starting.StartAsync(manager, cancellationToken)
                : new ValueTask<bool>(false);
        }
    }

    /// <summary>
    /// A state without a connection in which a background routine connects.
    /// </summary>
    private abstract class Connecting(TaskCompletionSource<IConnection> ready, long since) : Waiting(ready)
    {
        private Task? _routine;
        private object? _routineLock;

        /// <summary>
        /// Gets the timestamp since which no connection exists.
        /// </summary>
        public long Since { get; } = since;

        /// <summary>
        /// Gets the routine of the state, and starts it on the first call.
        /// </summary>
        public Task Run(RabbitMQConnectionManager manager)
            => LazyInitializer.EnsureInitialized(
                ref _routine,
                ref _routineLock,
                () =>
                {
                    // The routine does not run on the caller, which can be the reader loop of the client.
                    var routine = Task.Run(() => RunAsync(manager));

                    // Reading the exception marks it as observed when every caller cancelled its wait.
                    routine.ContinueWith(
                        static task => _ = task.Exception,
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);

                    return routine;
                });

        protected abstract Task RunAsync(RabbitMQConnectionManager manager);

        /// <summary>
        /// Gets the time the next attempt can take.
        /// </summary>
        protected virtual TimeSpan GetAttemptTimeout(RabbitMQConnectionManager manager)
            => manager._options.AttemptTimeout;

        /// <summary>
        /// Gets whether the state stops connecting after <paramref name="failure"/>, when the next attempt
        /// would begin after <paramref name="retryDelay"/>.
        /// </summary>
        protected virtual bool GivesUpAfter(RabbitMQConnectionManager manager, Exception failure, TimeSpan retryDelay)
            => false;

        /// <summary>
        /// Connects, retrying with exponential backoff, until an attempt succeeds or the state gives up, and
        /// replaces the state with the connected state.
        /// </summary>
        protected async Task ConnectAsync(RabbitMQConnectionManager manager)
        {
            var loopToken = manager._disposeCts.Token;

            for (var attempt = 1; ; attempt++)
            {
                loopToken.ThrowIfCancellationRequested();

                var attemptTimeout = GetAttemptTimeout(manager);
                using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(loopToken);
                attemptCts.CancelAfter(attemptTimeout);

                try
                {
                    manager._logger.ConnectionAttemptStarted(attempt);

                    var (connection, alarm) = await manager.ConnectOnceAsync(attemptCts.Token);

                    if (!manager.TryEnter(this, new Connected(connection, alarm)))
                    {
                        // The manager was disposed while the attempt ran. Disposal cancelled the routine's
                        // token before it replaced the state, so this ends the routine.
                        await manager.CloseConnectionAsync(connection);
                        loopToken.ThrowIfCancellationRequested();
                        return;
                    }

                    Ready.TrySetResult(connection);
                    manager._logger.ConnectionReady(attempt, manager._timeProvider.GetElapsedTime(Since));

                    // A connection that closed before it was handed out raised its shutdown event to nobody.
                    if (!connection.IsOpen)
                    {
                        manager.OnConnectionLost(connection, connection.GetCloseReason());
                    }

                    return;
                }
                catch (Exception ex) when (!loopToken.IsCancellationRequested)
                {
                    // Nobody cancelled the routine, so a cancellation is a timeout: of the attempt, or of the
                    // client, which reports a request the broker did not answer in time as a cancellation.
                    var failure =
                        ex is not OperationCanceledException ? ex
                        : attemptCts.IsCancellationRequested ? ThrowHelper.ConnectionAttemptTimedOut(attemptTimeout, ex)
                        : ThrowHelper.ConnectionRequestTimedOut(ex);
                    var delay = ExponentialBackoff.CalculateDelay(
                        attempt,
                        manager._options.InitialRetryDelay,
                        manager._options.MaxRetryDelay);
                    var elapsed = manager._timeProvider.GetElapsedTime(Since);

                    if (GivesUpAfter(manager, failure, delay))
                    {
                        manager._logger.ConnectionFailed(failure, attempt, elapsed);
                        throw;
                    }

                    manager._logger.ConnectionAttemptFailed(failure, attempt, elapsed, delay);

                    await Task.Delay(delay, manager._timeProvider, loopToken);
                }
            }
        }

        public override async Task DisposeAsync(RabbitMQConnectionManager manager)
        {
            await base.DisposeAsync(manager);

            try
            {
                await Run(manager);
            }
            catch
            {
                // The routine ended with the cancellation of the disposal, or failed the start it belonged to.
            }
        }
    }

    /// <summary>
    /// A start connects. It has a deadline and fails on a failure that repeats on every attempt.
    /// </summary>
    private sealed class Starting(TaskCompletionSource<IConnection> ready, long since) : Connecting(ready, since)
    {
        public override async ValueTask<bool> StartAsync(
            RabbitMQConnectionManager manager,
            CancellationToken cancellationToken)
        {
            try
            {
                await Run(manager).WaitAsync(cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
                when (manager._disposeCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw ThrowHelper.ConnectionManagerDisposed();
            }
        }

        protected override async Task RunAsync(RabbitMQConnectionManager manager)
        {
            try
            {
                await ConnectAsync(manager);
            }
            catch (Exception ex)
            {
                // Callers that wait for a connection keep waiting, for the connection of a later start.
                manager.TryEnter(this, new NotStarted(Ready));

                // Without a disposal, a cancellation is the timeout of the last attempt.
                if (ex is OperationCanceledException
                    && !manager._disposeCts.IsCancellationRequested)
                {
                    throw ThrowHelper.ConnectionTimedOut(manager._options.StartTimeout, ex);
                }

                throw;
            }
        }

        /// <summary>
        /// Gets the attempt timeout, cut to what remains of the start timeout. A start timeout of zero
        /// allows one whole attempt, and an infinite one, which is negative, sets no end.
        /// </summary>
        protected override TimeSpan GetAttemptTimeout(RabbitMQConnectionManager manager)
        {
            var attemptTimeout = base.GetAttemptTimeout(manager);
            var startTimeout = manager._options.StartTimeout;
            if (startTimeout <= TimeSpan.Zero)
            {
                return attemptTimeout;
            }

            var remaining = startTimeout - manager._timeProvider.GetElapsedTime(Since);
            if (remaining >= attemptTimeout)
            {
                return attemptTimeout;
            }

            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        /// <summary>
        /// Gets whether the failure repeats on every attempt, or the next attempt would begin after a start
        /// timeout that is not infinite.
        /// </summary>
        protected override bool GivesUpAfter(RabbitMQConnectionManager manager, Exception failure, TimeSpan retryDelay)
        {
            var startTimeout = manager._options.StartTimeout;
            return failure.IsPermanentFailure()
                || (
                    startTimeout != Timeout.InfiniteTimeSpan
                    && manager._timeProvider.GetElapsedTime(Since) + retryDelay > startTimeout);
        }
    }

    /// <summary>
    /// A connection is ready.
    /// </summary>
    private sealed class Connected(IConnection connection, Alarm alarm) : State
    {
        public override IConnection? Connection => connection;

        /// <summary>
        /// Gets the alarm of the connection.
        /// </summary>
        public Alarm Alarm => alarm;

        public override bool IsBlocked => connection.IsOpen && alarm.IsActive;

        public override Task WaitWhileBlockedAsync(CancellationToken cancellationToken)
        {
            var ended = alarm.Ended;
            return ended.IsCompleted ? Task.CompletedTask : ended.WaitAsync(cancellationToken);
        }

        public override ValueTask<bool> StartAsync(
            RabbitMQConnectionManager manager,
            CancellationToken cancellationToken)
            => new(IsOpenOrLost(manager));

        public override ValueTask<IConnection?> GetConnectionAsync(
            RabbitMQConnectionManager manager,
            CancellationToken cancellationToken)
            => new(IsOpenOrLost(manager) ? connection : null);

        public override Task DisposeAsync(RabbitMQConnectionManager manager)
        {
            alarm.End(retire: true);
            return manager.CloseConnectionAsync(connection);
        }

        /// <summary>
        /// Gets whether the connection is open, and reports the loss of a connection that closed before its
        /// shutdown event arrived.
        /// </summary>
        private bool IsOpenOrLost(RabbitMQConnectionManager manager)
        {
            if (connection.IsOpen)
            {
                return true;
            }

            manager.OnConnectionLost(connection, connection.GetCloseReason());
            return false;
        }
    }

    /// <summary>
    /// The connection was lost and a recovery connects. It has no deadline and never gives up.
    /// </summary>
    private sealed class Recovering(IConnection lostConnection, long since)
        : Connecting(new(TaskCreationOptions.RunContinuationsAsynchronously), since)
    {
        public override async ValueTask<bool> StartAsync(
            RabbitMQConnectionManager manager,
            CancellationToken cancellationToken)
        {
            // The start waits for the recovery as long as it would have kept trying itself, which is without
            // a limit for an infinite timeout.
            var startTimeout = manager._options.StartTimeout;
            try
            {
                await Ready.Task.WaitAsync(startTimeout, manager._timeProvider, cancellationToken);
                return true;
            }
            catch (TimeoutException ex)
            {
                throw ThrowHelper.ConnectionTimedOut(startTimeout, ex);
            }
        }

        protected override async Task RunAsync(RabbitMQConnectionManager manager)
        {
            // The lost connection is closed first, so the broker has released what belongs to it, such as an
            // exclusive queue. Disposing it also stops the client's own automatic recovery.
            await manager.CloseConnectionAsync(lostConnection);
            await ConnectAsync(manager);
        }
    }

    /// <summary>
    /// The manager was disposed. This state is final.
    /// </summary>
    private sealed class Disposed : State
    {
        public override ValueTask<bool> StartAsync(
            RabbitMQConnectionManager manager,
            CancellationToken cancellationToken)
            => throw ThrowHelper.ConnectionManagerDisposed();

        public override ValueTask<IConnection?> GetConnectionAsync(
            RabbitMQConnectionManager manager,
            CancellationToken cancellationToken)
            => throw ThrowHelper.ConnectionManagerDisposed();

        public override Task DisposeAsync(RabbitMQConnectionManager manager) => Task.CompletedTask;
    }

    /// <summary>
    /// Tracks whether the broker blocks one connection.
    /// </summary>
    private sealed class Alarm
    {
        private readonly object _sync = new();
        private TaskCompletionSource? _ended;
        private long _since;
        private bool _isRetired;

        /// <summary>
        /// Gets whether the broker blocks the connection.
        /// </summary>
        public bool IsActive => Volatile.Read(ref _ended) is not null;

        /// <summary>
        /// Gets a task that completes when the broker no longer blocks the connection.
        /// </summary>
        public Task Ended => Volatile.Read(ref _ended)?.Task ?? Task.CompletedTask;

        /// <summary>
        /// Records that the broker blocked the connection at <paramref name="timestamp"/>.
        /// </summary>
        public void Begin(long timestamp)
        {
            lock (_sync)
            {
                if (_isRetired || _ended is not null)
                {
                    return;
                }

                _since = timestamp;
                Volatile.Write(
                    ref _ended,
                    new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
            }
        }

        /// <summary>
        /// Ends the block and returns the timestamp it began at, or <c>null</c> when the connection was not
        /// blocked. With <paramref name="retire"/> the alarm ignores every later block.
        /// </summary>
        public long? End(bool retire = false)
        {
            TaskCompletionSource? ended;
            long since;
            lock (_sync)
            {
                _isRetired |= retire;
                ended = _ended;
                since = _since;
                Volatile.Write(ref _ended, null);
            }

            ended?.TrySetResult();
            return ended is null ? null : since;
        }
    }
}

file static class Extensions
{
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
