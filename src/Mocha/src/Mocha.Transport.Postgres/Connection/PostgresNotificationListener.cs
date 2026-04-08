using System.Collections.Immutable;
using System.Data;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Mocha.Transport.Postgres;

/// <summary>
/// Listens for PostgreSQL LISTEN/NOTIFY events on a specified channel and dispatches
/// notifications to registered subscribers. Used to signal queue receivers when new
/// messages are available.
/// </summary>
/// <remarks>
/// When the connection drops, the listener automatically reconnects with exponential
/// backoff (1s to 30s) and broadcasts an empty-string notification to all subscribers
/// so that receive endpoints re-poll their queues.
/// </remarks>
public sealed class PostgresNotificationListener : IAsyncDisposable
{
    private readonly string _channel;
    private readonly PostgresConnectionManager _connectionManager;
    private readonly ILogger<PostgresNotificationListener> _logger;

    private ImmutableArray<Action<string>> _subscribers = [];
    private NpgsqlConnection? _connection;
    private CancellationTokenSource? _cts;
    private Task _listenTask = Task.CompletedTask;
    private bool _isDisposed;

    public PostgresNotificationListener(
        PostgresConnectionManager connectionManager,
        IReadOnlyPostgresSchemaOptions schemaOptions,
        ILogger<PostgresNotificationListener> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
        _channel = schemaOptions.NotificationChannel;
    }

    public IDisposable Subscribe(Action<string> onNotification)
    {
        ImmutableInterlocked.Update(ref _subscribers, static (s, sub) => s.Add(sub), onNotification);

        return new Subscription(this, onNotification);
    }

    private void Unsubscribe(Action<string> onNotification)
    {
        ImmutableInterlocked.Update(ref _subscribers, static (s, sub) => s.Remove(sub), onNotification);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            return;
        }

        _connection = await _connectionManager.OpenConnectionAsync(cancellationToken);

        await using var listenCmd = _connection.CreateCommand();

        listenCmd.CommandText = $"LISTEN \"{_channel}\";";

        await listenCmd.ExecuteNonQueryAsync(cancellationToken);

        _cts = new CancellationTokenSource();
        _listenTask = ListenAsync(_cts.Token);
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_connection is null)
            {
                return;
            }

            _connection.Notification += OnNotification;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _connection.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Connection dropped - enter reconnection loop
                    _logger.NotificationListenerFailed(ex);

                    _connection.Notification -= OnNotification;

                    await ReconnectAsync(cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Graceful shutdown
        }
        finally
        {
            if (_connection is not null)
            {
                _connection.Notification -= OnNotification;
            }
        }
    }

    private async Task ReconnectAsync(CancellationToken cancellationToken)
    {
        // Dispose old connection
        if (_connection is not null)
        {
            try
            {
                await _connection.DisposeAsync();
            }
            catch
            {
                // Best-effort cleanup
            }

            _connection = null;
        }

        var attempt = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            attempt++;
            // Exponential backoff with jitter: ~2s, ~4s, ~8s, ~16s, ~30s
            var baseDelay = Math.Min((int)Math.Pow(2, Math.Min(attempt, 5)), 30);
            var delaySeconds = baseDelay * (0.5 + Random.Shared.NextDouble());

            try
            {
                await Task.Delay(TimeSpan.FromSeconds((int)delaySeconds), cancellationToken);

                _connection = await _connectionManager.OpenConnectionAsync(cancellationToken);

                await using var listenCmd = _connection.CreateCommand();

                listenCmd.CommandText = $"LISTEN \"{_channel}\";";

                await listenCmd.ExecuteNonQueryAsync(cancellationToken);

                _connection.Notification += OnNotification;

                _logger.NotificationListenerReconnected(attempt);

                // Signal all subscribers to re-poll their queues
                NotifySubscribers(string.Empty);

                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception retryEx)
            {
                _logger.NotificationListenerReconnectFailed(retryEx, attempt, (int)delaySeconds);

                // Dispose failed connection attempt
                if (_connection is not null)
                {
                    try
                    {
                        await _connection.DisposeAsync();
                    }
                    catch
                    {
                        // Best-effort cleanup
                    }

                    _connection = null;
                }
            }
        }
    }

    private void OnNotification(object sender, NpgsqlNotificationEventArgs e)
    {
        if (e.Channel != _channel)
        {
            return;
        }

        NotifySubscribers(e.Payload);
    }

    private void NotifySubscribers(string payload)
    {
        foreach (var subscriber in _subscribers.AsSpan())
        {
            try
            {
                subscriber(payload);
            }
            catch (Exception ex)
            {
                _logger.NotificationSubscriberFailed(ex);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_cts is not null)
        {
            await _cts.CancelAsync();
        }

        try
        {
            await _listenTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException)
        {
            // Proceed with cleanup
        }

        _cts?.Dispose();

        if (_connection is not null)
        {
            try
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await using var unlistenCmd = _connection.CreateCommand();
                    unlistenCmd.CommandText = $"UNLISTEN \"{_channel}\";";
                    await unlistenCmd.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                // Best-effort cleanup - connection may already be broken
            }
            finally
            {
                await _connection.DisposeAsync();
            }
        }

        _isDisposed = true;
    }

    private sealed class Subscription(PostgresNotificationListener listener, Action<string> onNotification)
        : IDisposable
    {
        private bool _isDisposed;

        public void Dispose()
        {
            if (!_isDisposed)
            {
                listener.Unsubscribe(onNotification);
                _isDisposed = true;
            }
        }
    }
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Error, "Error in PostgreSQL notification listener.")]
    public static partial void NotificationListenerFailed(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Error in notification subscriber.")]
    public static partial void NotificationSubscriberFailed(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "PostgreSQL notification listener reconnected after {Attempts} attempt(s).")]
    public static partial void NotificationListenerReconnected(this ILogger logger, int attempts);

    [LoggerMessage(
        LogLevel.Warning,
        "PostgreSQL notification listener reconnect attempt {Attempt} failed. Retrying in {DelaySeconds}s.")]
    public static partial void NotificationListenerReconnectFailed(
        this ILogger logger,
        Exception exception,
        int attempt,
        int delaySeconds);
}
