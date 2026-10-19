using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Base class for RabbitMQ connection management with reconnection, exponential backoff, and connection event
/// handling.
/// </summary>
/// <remarks>
/// The manager owns recovery: it disposes a connection that the broker or the network took away, which stops the
/// client's automatic recovery for that connection, and creates a new connection instead.
/// </remarks>
public abstract class RabbitMQConnectionManagerBase : IAsyncDisposable
{
    private const int MaxConnectionAttempts = 5;
    private static readonly TimeSpan s_initialRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan s_maxBackoffDelay = TimeSpan.FromSeconds(10);

    private readonly Func<CancellationToken, ValueTask<IConnection>> _connectionFactory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly CancellationTokenSource _disposeCts = new();

    private IConnection? _currentConnection;
    private Task? _reconnectTask;
    private int _isReconnecting;
    private bool _isDisposed;

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
    }

    /// <summary>
    /// Gets the logger used for connection lifecycle events.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Gets a value indicating whether the current connection is open and usable.
    /// </summary>
    public bool IsConnected => _currentConnection?.IsOpen ?? false;

    /// <summary>
    /// Gets the current underlying RabbitMQ connection, or <c>null</c> if no connection has been established.
    /// </summary>
    protected IConnection? CurrentConnection => _currentConnection;

    /// <summary>
    /// Gets a value indicating whether this manager has been disposed.
    /// </summary>
    protected bool IsDisposed => _isDisposed;

    /// <summary>
    /// Gets the current connection, creating one if necessary.
    /// </summary>
    public async ValueTask<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var connection = Volatile.Read(ref _currentConnection);
        if (connection is { IsOpen: true })
        {
            return connection;
        }

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            connection = _currentConnection;
            if (connection is { IsOpen: true })
            {
                return connection;
            }

            await CreateConnectionWithRetryAsync(cancellationToken);
            return _currentConnection!;
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
    /// Makes one attempt to connect, and keeps connecting in the background when the broker cannot be reached.
    /// </summary>
    /// <exception cref="Exception">
    /// The broker refused the credentials, the access to the virtual host, or an operation on an open connection,
    /// which retrying cannot resolve.
    /// </exception>
    internal async Task ConnectOrRetryInBackgroundAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _connectionLock.WaitAsync(cancellationToken);
            try
            {
                if (_currentConnection is not { IsOpen: true })
                {
                    await ConnectAsync(cancellationToken);
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested && IsConnectivityFailure(ex))
        {
            Logger.BrokerUnreachableConnectingInBackground(ex);
            StartReconnecting();
        }
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

    /// <summary>
    /// Called before a new connection is created.
    /// Override to perform cleanup (e.g., clearing channel pools).
    /// </summary>
    protected virtual Task OnBeforeConnectionCreatedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called after a new connection is successfully created and setup actions have run.
    /// Override to perform post-connection setup (e.g., reconnecting consumers).
    /// </summary>
    protected virtual Task OnAfterConnectionCreatedAsync(IConnection connection, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the connection is shut down (not by application).
    /// Override to perform cleanup.
    /// </summary>
    protected virtual Task OnConnectionLostAsync()
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

        _reconnectTask = ReconnectAsync(_disposeCts.Token);
    }

    private async Task ReconnectAsync(CancellationToken cancellationToken)
    {
        var attempt = 0;

        try
        {
            while (true)
            {
                TimeSpan delay;

                await _connectionLock.WaitAsync(cancellationToken);
                try
                {
                    if (_currentConnection is { IsOpen: true })
                    {
                        return;
                    }

                    await ConnectAsync(cancellationToken);
                    return;
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    attempt++;
                    delay = CalculateBackoffDelay(attempt);
                    Logger.ReconnectionAttemptFailed(ex, attempt, delay);
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
        if (!cancellationToken.IsCancellationRequested && _currentConnection is not { IsOpen: true })
        {
            StartReconnecting();
        }
    }

    /// <summary>
    /// Creates connection using the factory delegate with exponential backoff retry.
    /// Must be called while holding _connectionLock.
    /// </summary>
    private async Task CreateConnectionWithRetryAsync(CancellationToken cancellationToken)
    {
        var attempt = 0;

        while (true)
        {
            try
            {
                await ConnectAsync(cancellationToken);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                attempt++;

                if (attempt >= MaxConnectionAttempts)
                {
                    Logger.FailedToCreateConnectionAfterAttempts(ex, MaxConnectionAttempts);
                    throw;
                }

                var delay = CalculateBackoffDelay(attempt);
                Logger.FailedToCreateConnectionRetrying(ex, attempt, MaxConnectionAttempts, delay);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Replaces the current connection with a new one and runs the connection setup.
    /// Must be called while holding _connectionLock.
    /// </summary>
    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        Logger.CreatingConnectionUsingFactoryDelegate();

        await DisposeConnectionInternalAsync();
        await OnBeforeConnectionCreatedAsync(cancellationToken);

        var connection = await _connectionFactory(cancellationToken);
        WireConnectionEvents(connection);
        _currentConnection = connection;

        Logger.SuccessfullyCreatedConnection(connection.ClientProvidedName ?? "Unknown");

        try
        {
            // Declare exchanges, queues and bindings, then let derived classes attach to the connection.
            await OnConnectionEstablished(connection, cancellationToken);
            await OnAfterConnectionCreatedAsync(connection, cancellationToken);
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
        connection.CallbackExceptionAsync += OnCallbackExceptionAsync;
        connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
        connection.ConnectionBlockedAsync += OnConnectionBlockedAsync;
        connection.ConnectionUnblockedAsync += OnConnectionUnblockedAsync;
    }

    private void UnwireConnectionEvents(IConnection connection)
    {
        connection.CallbackExceptionAsync -= OnCallbackExceptionAsync;
        connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;
        connection.ConnectionBlockedAsync -= OnConnectionBlockedAsync;
        connection.ConnectionUnblockedAsync -= OnConnectionUnblockedAsync;
    }

    protected virtual Task OnConnectionEstablished(IConnection connection, CancellationToken cancellationToken)
        => Task.CompletedTask;

    private Task OnCallbackExceptionAsync(object? sender, CallbackExceptionEventArgs e)
    {
        Logger.ExceptionInConnectionCallback(e.Exception, e.Detail.Print());

        return Task.CompletedTask;
    }

    private Task OnConnectionShutdownAsync(object? sender, ShutdownEventArgs e)
    {
        if (e.Initiator == ShutdownInitiator.Application)
        {
            Logger.ConnectionClosedByApplication();
            return Task.CompletedTask;
        }

        Logger.ConnectionShutdownDetected(e.Initiator, e.ReplyText);

        // The client raises this event on its reader loop or heartbeat timer, which cannot process the
        // replies that closing a channel waits for, so the cleanup must not run inline.
        _ = Task.Run(HandleConnectionLostAsync);
        return Task.CompletedTask;
    }

    private async Task HandleConnectionLostAsync()
    {
        try
        {
            await _connectionLock.WaitAsync(_disposeCts.Token);
            try
            {
                // Disposing stops the client's automatic recovery, which would otherwise compete with this manager.
                if (_currentConnection is { IsOpen: false })
                {
                    await DisposeConnectionInternalAsync();
                }
            }
            finally
            {
                _connectionLock.Release();
            }

            await OnConnectionLostAsync();
        }
        catch (Exception) when (_isDisposed)
        {
            // The manager was disposed.
        }
        catch (Exception ex)
        {
            Logger.ErrorHandlingConnectionLoss(ex);
        }
    }

    private Task OnConnectionBlockedAsync(object? sender, ConnectionBlockedEventArgs e)
    {
        Logger.ConnectionBlocked(e.Reason);
        return Task.CompletedTask;
    }

    private Task OnConnectionUnblockedAsync(object? sender, AsyncEventArgs e)
    {
        Logger.ConnectionUnblocked();
        return Task.CompletedTask;
    }

    private static TimeSpan CalculateBackoffDelay(int attempt)
    {
        var delay = s_initialRetryDelay * Math.Pow(2, Math.Min(attempt - 1, 16));
        return delay > s_maxBackoffDelay ? s_maxBackoffDelay : delay;
    }

    private async Task DisposeConnectionInternalAsync()
    {
        if (_currentConnection is null)
        {
            return;
        }

        try
        {
            UnwireConnectionEvents(_currentConnection);

            if (_currentConnection.IsOpen)
            {
                await _currentConnection.CloseAsync();
            }

            await _currentConnection.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.ErrorDisposingConnection(ex);
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
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        await _disposeCts.CancelAsync();

        // Allow derived classes to clean up first
        await DisposeAsyncCore();

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

        // Dispose connection
        await DisposeConnectionInternalAsync();

        _connectionLock.Dispose();
        _disposeCts.Dispose();
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
