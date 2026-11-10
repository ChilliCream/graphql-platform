using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Mocha.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Manages RabbitMQ connections with retries and reconnection.
/// Recovery is managed independently of the connection factory's automatic recovery setting.
/// </summary>
public abstract partial class RabbitMQConnectionManagerBase : IAsyncDisposable
{
    private static readonly TimeSpan s_initialRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan s_maxBackoffDelay = TimeSpan.FromSeconds(10);

    private readonly Func<CancellationToken, ValueTask<IConnection>> _connectionFactory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly CancellationToken _disposeToken;
    private readonly object _disposeSync = new();
    private Task? _disposeTask;

    private IConnection? _currentConnection;
    private IConnection? _readyConnection;
    private AsyncEventHandler<ShutdownEventArgs>? _currentShutdownHandler;
    private Task? _reconnectTask;
    private int _isReconnecting;
    private bool _hasEstablishedConnection;
    private long? _connectionStartedAt;
    private volatile bool _isDisposed;

    /// <summary>
    /// Creates a new connection manager base with the specified logger and connection factory.
    /// </summary>
    /// <param name="logger">The logger for connection lifecycle events.</param>
    /// <param name="connectionFactory">A factory delegate that creates new RabbitMQ connections on demand.</param>
    protected RabbitMQConnectionManagerBase(
        ILogger logger,
        Func<CancellationToken, ValueTask<IConnection>> connectionFactory)
    {
        Logger = logger;
        _connectionFactory = connectionFactory;
        _disposeToken = _disposeCts.Token;
        ConnectionName = GetType().Name;
    }

    internal string ConnectionName { get; init; }

    /// <summary>
    /// Gets the clock used for connection and consumer diagnostic timestamps.
    /// </summary>
    internal TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    /// <summary>
    /// Gets the logger used for connection lifecycle events.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Gets a value indicating whether the current connection is open and usable.
    /// </summary>
    public bool IsConnected => Volatile.Read(ref _readyConnection)?.IsOpen ?? false;

    /// <summary>
    /// Gets the current underlying RabbitMQ connection, or <c>null</c> if no connection has been established.
    /// </summary>
    protected IConnection? CurrentConnection => _currentConnection;

    /// <summary>
    /// Gets a value indicating whether this manager has been disposed.
    /// </summary>
    protected bool IsDisposed => _isDisposed;

    /// <summary>
    /// Gets a token cancelled when disposal begins.
    /// </summary>
    protected CancellationToken DisposalToken => _disposeToken;

    /// <summary>
    /// Gets or sets how long connecting keeps retrying while the broker cannot be reached, before it fails.
    /// </summary>
    internal TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets a connection whose setup has completed, creating one if necessary.
    /// </summary>
    public async ValueTask<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var connection = Volatile.Read(ref _readyConnection);
        if (connection is { IsOpen: true })
        {
            return connection;
        }

        using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeToken);
        await _connectionLock.WaitAsync(waitCts.Token);
        try
        {
            waitCts.Token.ThrowIfCancellationRequested();
            // Double-check after acquiring lock
            connection = _readyConnection;
            if (connection is { IsOpen: true })
            {
                return connection;
            }

            await CreateConnectionWithRetryAsync(cancellationToken);
            return _readyConnection!;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Ensures connection is established, creating it if necessary.
    /// </summary>
    public async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        await GetConnectionAsync(cancellationToken);
    }

    /// <summary>
    /// Called before an existing connection is replaced or a new connection is created.
    /// </summary>
    protected virtual Task OnBeforeConnectionCreatedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called after a new connection is created and its initial setup completes.
    /// </summary>
    protected virtual Task OnAfterConnectionCreatedAsync(IConnection connection, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a connection is lost without an application-requested shutdown.
    /// </summary>
    protected virtual Task OnConnectionLostAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called after a previously established connection has been replaced and the replacement is ready for use.
    /// </summary>
    protected virtual Task OnConnectionRecoveredAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts establishing a connection in the background, retrying with exponential backoff until a connection
    /// is open or the manager is disposed. Does nothing while such an attempt is already running.
    /// </summary>
    protected void StartReconnecting()
    {
        if (_isDisposed || Interlocked.CompareExchange(ref _isReconnecting, 1, 0) != 0)
        {
            return;
        }

        _reconnectTask = ReconnectAsync(_disposeToken);
    }

    private async Task ReconnectAsync(CancellationToken cancellationToken)
    {
        var attempt = 0;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TimeSpan delay;

                await _connectionLock.WaitAsync(cancellationToken);
                try
                {
                    if (_readyConnection is { IsOpen: true })
                    {
                        break;
                    }

                    await ConnectAsync(cancellationToken);
                    break;
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    using var _ = BeginLogScope();
                    attempt++;
                    delay = ExponentialBackoff.CalculateDelay(attempt, s_initialRetryDelay, s_maxBackoffDelay);
                    Logger.ReconnectionAttemptFailed(
                        ex,
                        RabbitMQFailure.GetLogLevel(ex),
                        ConnectionName,
                        attempt,
                        delay,
                        TimeProvider.GetElapsedTime(_connectionStartedAt!.Value));
                }
                finally
                {
                    _connectionLock.Release();
                }

                await Task.Delay(delay, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The manager was disposed.
        }
        catch (ObjectDisposedException) when (_isDisposed)
        {
            // The manager was disposed.
        }
        finally
        {
            Volatile.Write(ref _isReconnecting, 0);
        }

        // A connection can be lost after the attempt succeeded and before the flag above was reset.
        if (!cancellationToken.IsCancellationRequested && _readyConnection is not { IsOpen: true })
        {
            StartReconnecting();
        }
    }

    /// <summary>
    /// Creates a connection, retrying with exponential backoff until <see cref="ConnectTimeout"/> elapses.
    /// Refused credentials, refused virtual host access and broker rejections fail at once.
    /// Must be called while holding _connectionLock.
    /// </summary>
    private async Task CreateConnectionWithRetryAsync(CancellationToken cancellationToken)
    {
        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeToken);
        var connectTimeout = ConnectTimeout;
        if (connectTimeout > TimeSpan.Zero)
        {
            connectCts.CancelAfter(connectTimeout);
        }

        var connectToken = connectCts.Token;
        var started = Stopwatch.GetTimestamp();
        var attempt = 0;

        try
        {
            while (!connectToken.IsCancellationRequested)
            {
                try
                {
                    await ConnectAsync(connectToken);
                    return;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    using var _ = BeginLogScope();
                    attempt++;
                    var delay = ExponentialBackoff.CalculateDelay(attempt, s_initialRetryDelay, s_maxBackoffDelay);

                    if (RabbitMQFailure.IsBrokerRejection(ex)
                        || Stopwatch.GetElapsedTime(started) + delay > connectTimeout)
                    {
                        Logger.FailedToCreateConnectionAfterAttempts(ex, ConnectionName, attempt);
                        throw;
                    }

                    Logger.FailedToCreateConnectionRetrying(
                        ex,
                        RabbitMQFailure.GetLogLevel(ex),
                        ConnectionName,
                        attempt,
                        delay);

                    await Task.Delay(delay, connectToken);
                }
            }

            connectToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException ex)
            when (connectCts.IsCancellationRequested
                && !cancellationToken.IsCancellationRequested
                && !_disposeToken.IsCancellationRequested)
        {
            using var _ = BeginLogScope();
            Logger.FailedToCreateConnectionAfterAttempts(ex, ConnectionName, attempt + 1);
            throw ThrowHelper.ConnectionTimedOut(connectTimeout, ex);
        }
    }

    /// <summary>
    /// Replaces the current connection with a new one and runs the connection setup.
    /// Must be called while holding _connectionLock.
    /// </summary>
    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        StartConnectionLogContext();
        using var attemptScope = BeginLogScope();
        Volatile.Write(ref _readyConnection, null);

        _connectionStartedAt ??= TimeProvider.GetTimestamp();

        Logger.CreatingConnectionUsingFactoryDelegate(ConnectionName);

        try
        {
            await OnBeforeConnectionCreatedAsync(cancellationToken);
        }
        finally
        {
            await DisposeConnectionInternalAsync();
        }

        var connection = await _connectionFactory(cancellationToken);

        TrackConnection(connection);
        using var connectionScope = BeginLogScope(connection);
        WireConnectionEvents(connection);
        _currentConnection = connection;

        Logger.ConnectionOpened(ConnectionName, connection.ClientProvidedName);

        try
        {
            _disposeToken.ThrowIfCancellationRequested();
            cancellationToken.ThrowIfCancellationRequested();
            // Declare exchanges, queues and bindings, then let derived classes attach to the connection.
            await OnConnectionEstablished(connection, cancellationToken);
            await OnAfterConnectionCreatedAsync(connection, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // A connection that closed before its shutdown handler was subscribed raises no event.
            if (!connection.IsOpen)
            {
                throw ThrowHelper.ConnectionClosedDuringSetup(connection.CloseReason);
            }

            var isRecovery = _hasEstablishedConnection;
            _hasEstablishedConnection = true;
            Volatile.Write(ref _readyConnection, connection);

            if (isRecovery)
            {
                await OnConnectionRecoveredAsync(cancellationToken);
                Logger.ConnectionRecovered(ConnectionName, TimeProvider.GetElapsedTime(_connectionStartedAt.Value));
            }
            else
            {
                Logger.ConnectionReady(ConnectionName, TimeProvider.GetElapsedTime(_connectionStartedAt.Value));
            }

            _connectionStartedAt = null;
        }
        catch
        {
            await DisposeConnectionInternalAsync();
            throw;
        }
    }

    private void WireConnectionEvents(IConnection connection)
    {
        AsyncEventHandler<ShutdownEventArgs> onShutdown = (_, e) => OnConnectionShutdownAsync(connection, e);
        _currentShutdownHandler = onShutdown;

        connection.CallbackExceptionAsync += OnCallbackExceptionAsync;
        connection.ConnectionBlockedAsync += OnConnectionBlockedAsync;
        connection.ConnectionUnblockedAsync += OnConnectionUnblockedAsync;
        connection.ConnectionShutdownAsync += onShutdown;
    }

    private void UnwireConnectionEvents(IConnection connection)
    {
        connection.CallbackExceptionAsync -= OnCallbackExceptionAsync;
        connection.ConnectionBlockedAsync -= OnConnectionBlockedAsync;
        connection.ConnectionUnblockedAsync -= OnConnectionUnblockedAsync;

        if (_currentShutdownHandler is not null)
        {
            connection.ConnectionShutdownAsync -= _currentShutdownHandler;
            _currentShutdownHandler = null;
        }
    }

    protected virtual Task OnConnectionEstablished(IConnection connection, CancellationToken cancellationToken)
        => Task.CompletedTask;

    private Task OnCallbackExceptionAsync(object? sender, CallbackExceptionEventArgs e)
    {
        using var _ = BeginLogScope(sender as IConnection);
        Logger.ExceptionInConnectionCallback(e.Exception, ConnectionName, e.Detail.Print());
        return Task.CompletedTask;
    }

    private Task OnConnectionShutdownAsync(IConnection connection, ShutdownEventArgs e)
    {
        if (e.Initiator == ShutdownInitiator.Application)
        {
            using var _ = BeginLogScope(connection);
            Logger.ConnectionClosedByApplication(ConnectionName);
            return Task.CompletedTask;
        }

        using (BeginLogScope(connection))
        {
            Logger.ConnectionShutdownDetected(ConnectionName, e.Initiator, e.ReplyCode, e.ReplyText);
        }

        // The client raises this event on its reader loop or heartbeat timer, which cannot process the reply
        // to a channel close.
        _ = Task.Run(() => HandleConnectionLostAsync(connection));
        return Task.CompletedTask;
    }

    private async Task HandleConnectionLostAsync(IConnection connection)
    {
        using var _ = BeginLogScope(connection);
        try
        {
            var isCurrent = false;

            await _connectionLock.WaitAsync(_disposeToken);
            try
            {
                // The event of a connection that was already replaced is ignored.
                if (ReferenceEquals(_currentConnection, connection))
                {
                    isCurrent = true;

                    // Disposing also stops the client's own automatic recovery.
                    await DisposeConnectionInternalAsync();
                }
            }
            finally
            {
                _connectionLock.Release();
            }

            if (isCurrent)
            {
                await OnConnectionLostAsync();
            }
        }
        catch (Exception) when (_isDisposed)
        {
            // The manager was disposed.
        }
        catch (Exception ex)
        {
            Logger.ErrorHandlingConnectionLoss(ex, ConnectionName);
        }
    }

    private Task OnConnectionBlockedAsync(object? sender, ConnectionBlockedEventArgs e)
    {
        using var _ = BeginLogScope(sender as IConnection);
        Logger.ConnectionBlocked(ConnectionName, e.Reason);
        return Task.CompletedTask;
    }

    private Task OnConnectionUnblockedAsync(object? sender, AsyncEventArgs e)
    {
        using var _ = BeginLogScope(sender as IConnection);
        Logger.ConnectionUnblocked(ConnectionName);
        return Task.CompletedTask;
    }

    private async Task DisposeConnectionInternalAsync()
    {
        Volatile.Write(ref _readyConnection, null);

        if (_currentConnection is null)
        {
            return;
        }

        using var _ = BeginLogScope(_currentConnection);
        try
        {
            UnwireConnectionEvents(_currentConnection);

            try
            {
                if (_currentConnection.IsOpen)
                {
                    await _currentConnection.CloseAsync();
                }
            }
            finally
            {
                await _currentConnection.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.ErrorDisposingConnection(ex, ConnectionName);
        }
        finally
        {
            _currentConnection = null;
        }
    }

    /// <summary>
    /// Closes and disposes a channel, ignoring failures of either step.
    /// </summary>
    internal static async Task CloseChannelAsync(IChannel channel, CancellationToken cancellationToken)
    {
        try
        {
            if (channel.IsOpen)
            {
                await channel.CloseAsync(cancellationToken: cancellationToken);
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
    /// Override to perform cleanup before base disposal.
    /// </summary>
    protected virtual ValueTask DisposeAsyncCore()
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Disposes the connection manager, calling <see cref="DisposeAsyncCore"/> for derived cleanup then closing the connection.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        lock (_disposeSync)
        {
            return new ValueTask(_disposeTask ??= DisposeCoreAsync());
        }
    }

    private async Task DisposeCoreAsync()
    {
        _isDisposed = true;
        await _disposeCts.CancelAsync();

        // Connection attempts in progress hold the lock.
        await _connectionLock.WaitAsync(CancellationToken.None);
        try
        {
            await DisposeAsyncCore();
        }
        finally
        {
            await DisposeConnectionInternalAsync();
            _connectionLock.Release();
        }

        if (_reconnectTask is not null)
        {
            try
            {
                await _reconnectTask;
            }
            catch
            {
                // Ignore
            }
        }

        // The semaphore remains valid until all cancelled waiters have exited.
        _disposeCts.Dispose();
        using var _ = BeginLogScope();
        Logger.ConnectionManagerStopped(ConnectionName);
    }
}

file static class Extensions
{
    public static string Print(this IDictionary<string, object>? obj)
    {
        var sb = new StringBuilder();
        foreach (var item in obj ?? Enumerable.Empty<KeyValuePair<string, object>>())
        {
            sb.AppendLine($"{item.Key}: {item.Value}");
        }
        return sb.ToString();
    }
}
