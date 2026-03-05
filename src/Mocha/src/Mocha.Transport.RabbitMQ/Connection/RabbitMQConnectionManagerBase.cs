using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Base class for RabbitMQ connection management with automatic reconnection,
/// exponential backoff, and connection event handling.
/// </summary>
public abstract class RabbitMQConnectionManagerBase : IAsyncDisposable
{
    private const int InitialConnectionRetryDelaySeconds = 1;
    private const int MaxConnectionAttempts = 5;
    private static readonly TimeSpan MaxBackoffDelay = TimeSpan.FromSeconds(60);

    private readonly Func<CancellationToken, ValueTask<IConnection>> _connectionFactory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private IConnection? _currentConnection;
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
    /// Called after automatic recovery succeeds.
    /// Override to perform post-recovery actions.
    /// </summary>
    protected virtual Task OnConnectionRecoveredAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates connection using the factory delegate with exponential backoff retry.
    /// Must be called while holding _connectionLock.
    /// </summary>
    private async Task CreateConnectionWithRetryAsync(CancellationToken cancellationToken)
    {
        var attempt = 0;
        var baseDelay = TimeSpan.FromSeconds(InitialConnectionRetryDelaySeconds);

        Logger.CreatingConnectionUsingFactoryDelegate();

        while (attempt < MaxConnectionAttempts)
        {
            try
            {
                // Clean up existing connection if any
                await DisposeConnectionInternalAsync();

                // Allow derived classes to perform cleanup
                await OnBeforeConnectionCreatedAsync(cancellationToken);

                // Create new connection
                var connection = await _connectionFactory(cancellationToken);

                // Wire up connection events
                WireConnectionEvents(connection);

                _currentConnection = connection;

                Logger.SuccessfullyCreatedConnection(connection.ClientProvidedName ?? "Unknown");

                // Run setup actions (declare exchanges, queues, etc.)
                await OnConnectionEstablished(connection, cancellationToken);

                // Allow derived classes to perform post-connection setup
                await OnAfterConnectionCreatedAsync(connection, cancellationToken);

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

                var delay = CalculateBackoffDelay(baseDelay, attempt);
                Logger.FailedToCreateConnectionRetrying(ex, attempt, MaxConnectionAttempts, delay);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private void WireConnectionEvents(IConnection connection)
    {
        connection.CallbackExceptionAsync += OnCallbackExceptionAsync;
        connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
        connection.RecoverySucceededAsync += OnRecoverySucceededAsync;
        connection.ConnectionRecoveryErrorAsync += OnConnectionRecoveryErrorAsync;
        connection.ConnectionBlockedAsync += OnConnectionBlockedAsync;
        connection.ConnectionUnblockedAsync += OnConnectionUnblockedAsync;
    }

    private void UnwireConnectionEvents(IConnection connection)
    {
        connection.CallbackExceptionAsync -= OnCallbackExceptionAsync;
        connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;
        connection.RecoverySucceededAsync -= OnRecoverySucceededAsync;
        connection.ConnectionRecoveryErrorAsync -= OnConnectionRecoveryErrorAsync;
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

    private async Task OnConnectionShutdownAsync(object? sender, ShutdownEventArgs e)
    {
        if (e.Initiator == ShutdownInitiator.Application)
        {
            Logger.ConnectionClosedByApplication();
            return;
        }

        Logger.ConnectionShutdownDetected(e.Initiator, e.ReplyText);

        await OnConnectionLostAsync();
    }

    private async Task OnRecoverySucceededAsync(object? sender, AsyncEventArgs e)
    {
        Logger.ConnectionRecoverySucceeded();

        try
        {
            if (_currentConnection is not null)
            {
                await OnConnectionEstablished(_currentConnection, CancellationToken.None);
            }

            await OnConnectionRecoveredAsync(CancellationToken.None);

            Logger.SuccessfullyRecoveredConnectionTopologyAndConsumers();
        }
        catch (Exception ex)
        {
            Logger.ErrorDuringPostRecoveryOperations(ex);
        }
    }

    private Task OnConnectionRecoveryErrorAsync(object? sender, ConnectionRecoveryErrorEventArgs e)
    {
        Logger.ConnectionRecoveryFailedWillRetry(e.Exception, e.Exception?.Message);
        return Task.CompletedTask;
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

    private static TimeSpan CalculateBackoffDelay(TimeSpan baseDelay, int attempt)
    {
        var delay = baseDelay * Math.Pow(2, attempt - 1);
        return delay > MaxBackoffDelay ? MaxBackoffDelay : delay;
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

        // Allow derived classes to clean up first
        await DisposeAsyncCore();

        // Dispose connection
        await DisposeConnectionInternalAsync();

        _connectionLock.Dispose();
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
