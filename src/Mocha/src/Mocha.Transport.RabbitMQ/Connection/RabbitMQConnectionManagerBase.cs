using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using Microsoft.Extensions.Logging;
using Mocha.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

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

    private static bool IsConnectivityFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            // Refused credentials or virtual host access, and operations the broker rejects, such as a declare
            // with different arguments, fail the same way on every attempt.
            if (current is AuthenticationFailureException
                || current is OperationInterruptedException
                {
                    ShutdownReason.ReplyCode:
                        Constants.AccessRefused
                        or Constants.NotFound
                        or Constants.ResourceLocked
                        or Constants.PreconditionFailed
                        or Constants.NotAllowed
                })
            {
                return false;
            }
        }

        return true;
    }

    internal static LogLevel GetRecoveryFailureLogLevel(Exception exception)
    {
        if (exception is AggregateException aggregate)
        {
            return aggregate.InnerExceptions.Count > 0
                && aggregate.InnerExceptions.All(static ex => GetRecoveryFailureLogLevel(ex) == LogLevel.Warning)
                    ? LogLevel.Warning
                    : LogLevel.Error;
        }

        if (exception.InnerException is { } inner && GetRecoveryFailureLogLevel(inner) == LogLevel.Error)
        {
            return LogLevel.Error;
        }

        return exception switch
        {
            AuthenticationException or PossibleAuthenticationFailureException => LogLevel.Error,
            OperationInterruptedException
            {
                ShutdownReason.ReplyCode:
                    Constants.AccessRefused
                    or Constants.NotFound
                    or Constants.ResourceLocked
                    or Constants.PreconditionFailed
                    or Constants.NotAllowed
            } => LogLevel.Error,
            IOException or SocketException or TimeoutException or OperationCanceledException
                or ObjectDisposedException or BrokerUnreachableException or ConnectFailureException
                or OperationInterruptedException => LogLevel.Warning,
            _ => LogLevel.Error
        };
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
        var lastSummary = TimeProvider.GetTimestamp();
        var lastLevel = LogLevel.None;

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

                    var level = GetRecoveryFailureLogLevel(ex);
                    if (attempt == 1 || level > lastLevel
                        || TimeProvider.GetElapsedTime(lastSummary) >= TimeSpan.FromMinutes(1))
                    {
                        lastSummary = TimeProvider.GetTimestamp();
                        lastLevel = level;
                        Logger.ReconnectionAttemptFailed(
                            ex,
                            level,
                            ConnectionName,
                            attempt,
                            delay,
                            TimeProvider.GetElapsedTime(_connectionStartedAt!.Value));
                    }
                    else
                    {
                        Logger.ReconnectionAttemptFailedAgain(ex, ConnectionName, attempt, delay);
                    }
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

        // A connection lost between the successful attempt and the reset above is not reconnected otherwise.
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
        // A retry that outlives the manager would create a connection that nothing disposes.
        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeToken);
        var connectTimeout = ConnectTimeout;
        if (connectTimeout > TimeSpan.Zero)
        {
            connectCts.CancelAfter(connectTimeout);
        }

        var connectToken = connectCts.Token;
        var started = Stopwatch.GetTimestamp();
        var lastSummary = TimeProvider.GetTimestamp();
        var lastLevel = LogLevel.None;
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
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex) when (!IsConnectivityFailure(ex))
                {
                    using var _ = BeginLogScope();
                    Logger.FailedToCreateConnectionAfterAttempts(ex, ConnectionName, attempt + 1);
                    throw;
                }
                catch (Exception ex)
                {
                    using var _ = BeginLogScope();
                    attempt++;
                    var delay = ExponentialBackoff.CalculateDelay(attempt, s_initialRetryDelay, s_maxBackoffDelay);

                    if (Stopwatch.GetElapsedTime(started) + delay > connectTimeout)
                    {
                        Logger.FailedToCreateConnectionAfterAttempts(ex, ConnectionName, attempt);
                        throw;
                    }

                    var level = GetRecoveryFailureLogLevel(ex);
                    if (attempt == 1 || level > lastLevel
                        || TimeProvider.GetElapsedTime(lastSummary) >= TimeSpan.FromMinutes(1))
                    {
                        lastSummary = TimeProvider.GetTimestamp();
                        lastLevel = level;
                    }
                    else
                    {
                        level = LogLevel.Debug;
                    }

                    Logger.FailedToCreateConnectionRetrying(ex, level, ConnectionName, attempt, delay);

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
            // A connection without its topology must not be handed out, so the next attempt starts over.
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
        using var _ = sender is IConnection connection ? BeginLogScope(connection) : BeginLogScope();
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

        // The client raises this event on its reader loop or heartbeat timer, which cannot process the
        // replies that closing a channel waits for, so the cleanup must not run inline.
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
                // A late event from a connection that was already replaced must not disturb the current one.
                if (ReferenceEquals(_currentConnection, connection))
                {
                    isCurrent = true;

                    // Disposing stops the client's automatic recovery, and replaces a connection that the client
                    // already recovered with consumers this manager no longer tracks.
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
        using var _ = sender is IConnection connection ? BeginLogScope(connection) : BeginLogScope();
        Logger.ConnectionBlocked(ConnectionName, e.Reason);
        return Task.CompletedTask;
    }

    private Task OnConnectionUnblockedAsync(object? sender, AsyncEventArgs e)
    {
        using var _ = sender is IConnection connection ? BeginLogScope(connection) : BeginLogScope();
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

        // Foreground connection attempts and background recovery must finish before their resources are closed.
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

        // A reconnect that is still running could otherwise create a connection after the one below is disposed.
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

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Debug, "Creating RabbitMQ connection {ConnectionName}")]
    public static partial void CreatingConnectionUsingFactoryDelegate(this ILogger logger, string connectionName);

    [LoggerMessage(LogLevel.Debug, "RabbitMQ connection {ConnectionName} is open with client name {ClientProvidedName}; setup is pending")]
    public static partial void ConnectionOpened(this ILogger logger, string connectionName, string? clientProvidedName);

    [LoggerMessage(LogLevel.Information, "RabbitMQ connection {ConnectionName} is ready after {Elapsed}")]
    public static partial void ConnectionReady(this ILogger logger, string connectionName, TimeSpan elapsed);

    [LoggerMessage(LogLevel.Information, "RabbitMQ connection {ConnectionName} recovered after {Elapsed}")]
    public static partial void ConnectionRecovered(this ILogger logger, string connectionName, TimeSpan elapsed);

    [LoggerMessage(LogLevel.Error, "Failed to establish RabbitMQ connection {ConnectionName} after {Attempts} attempts")]
    public static partial void FailedToCreateConnectionAfterAttempts(
        this ILogger logger,
        Exception ex,
        string connectionName,
        int attempts);

    [LoggerMessage(Message = "Failed to establish RabbitMQ connection {ConnectionName} (attempt {Attempt}). Retrying in {Delay}")]
    public static partial void FailedToCreateConnectionRetrying(
        this ILogger logger,
        Exception ex,
        LogLevel level,
        string connectionName,
        int attempt,
        TimeSpan delay);

    [LoggerMessage(LogLevel.Debug, "RabbitMQ connection {ConnectionName} closed by application")]
    public static partial void ConnectionClosedByApplication(this ILogger logger, string connectionName);

    [LoggerMessage(LogLevel.Information, "RabbitMQ connection manager {ConnectionName} stopped")]
    public static partial void ConnectionManagerStopped(this ILogger logger, string connectionName);

    [LoggerMessage(LogLevel.Warning, "RabbitMQ connection {ConnectionName} lost. Initiator: {Initiator}, Reply: {ReplyCode} {ReplyText}")]
    public static partial void ConnectionShutdownDetected(
        this ILogger logger,
        string connectionName,
        ShutdownInitiator initiator,
        ushort replyCode,
        string replyText);

    [LoggerMessage(LogLevel.Error, "Error handling loss of RabbitMQ connection {ConnectionName}")]
    public static partial void ErrorHandlingConnectionLoss(this ILogger logger, Exception ex, string connectionName);

    [LoggerMessage(LogLevel.Warning, "RabbitMQ connection {ConnectionName} blocked by broker: {Reason}")]
    public static partial void ConnectionBlocked(this ILogger logger, string connectionName, string reason);

    [LoggerMessage(LogLevel.Information, "RabbitMQ connection {ConnectionName} unblocked by broker")]
    public static partial void ConnectionUnblocked(this ILogger logger, string connectionName);

    [LoggerMessage(Message = "RabbitMQ connection {ConnectionName} is still unavailable after {Elapsed} (attempt {Attempt}). Retrying in {Delay}")]
    public static partial void ReconnectionAttemptFailed(
        this ILogger logger,
        Exception ex,
        LogLevel level,
        string connectionName,
        int attempt,
        TimeSpan delay,
        TimeSpan elapsed);

    [LoggerMessage(LogLevel.Error, "Error disposing RabbitMQ connection {ConnectionName}")]
    public static partial void ErrorDisposingConnection(this ILogger logger, Exception ex, string connectionName);

    [LoggerMessage(LogLevel.Error, "Exception in RabbitMQ connection {ConnectionName} callback. Detail: {Detail}")]
    public static partial void ExceptionInConnectionCallback(
        this ILogger logger,
        Exception ex,
        string connectionName,
        string detail);

    [LoggerMessage(LogLevel.Debug, "RabbitMQ connection {ConnectionName} recovery attempt {Attempt} failed. Retrying in {Delay}")]
    public static partial void ReconnectionAttemptFailedAgain(
        this ILogger logger,
        Exception ex,
        string connectionName,
        int attempt,
        TimeSpan delay);
}
