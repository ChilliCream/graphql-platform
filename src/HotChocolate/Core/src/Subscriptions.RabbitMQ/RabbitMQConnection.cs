using HotChocolate.Subscriptions.Diagnostics;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using static HotChocolate.Subscriptions.RabbitMQ.RabbitMQResources;

namespace HotChocolate.Subscriptions.RabbitMQ;

internal sealed class RabbitMQConnection : IRabbitMQConnection, IAsyncDisposable
{
    private const int RetryCount = 6;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ISubscriptionDiagnosticEvents _diagnosticEvents;
    private readonly ConnectionFactory _connectionFactory;
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;

    public RabbitMQConnection(ISubscriptionDiagnosticEvents diagnosticEvents, ConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(diagnosticEvents);
        ArgumentNullException.ThrowIfNull(connectionFactory);

        _diagnosticEvents = diagnosticEvents;
        _connectionFactory = connectionFactory;
        _connectionFactory.TopologyRecoveryEnabled = true;
        _connectionFactory.AutomaticRecoveryEnabled = true;
    }

    public async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_channel?.IsOpen is true)
        {
            return _channel;
        }

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_channel?.IsOpen is true)
            {
                return _channel;
            }

            if (_channel != null)
            {
                await _channel.DisposeAsync().ConfigureAwait(false);
                _channel = null;
            }

            if (_connection is { IsOpen: false })
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
                _connection = null;
            }

            _connection ??= await CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            return _channel;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_channel != null)
        {
            await _channel.CloseAsync().ConfigureAwait(false);
            await _channel.DisposeAsync().ConfigureAwait(false);
        }

        if (_connection != null)
        {
            await _connection.CloseAsync().ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
        }

        _semaphore.Dispose();
        _disposed = true;
    }

    private async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        for (var connectionAttempt = 0; connectionAttempt < RetryCount; connectionAttempt++)
        {
            try
            {
                return await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (BrokerUnreachableException)
            {
                _diagnosticEvents.ProviderInfo(string.Format(
                    RabbitMQConnection_InitializeConnection_ConnectionAttemptFailed,
                    connectionAttempt));

                if (connectionAttempt < RetryCount - 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, connectionAttempt)), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        _diagnosticEvents.ProviderInfo(string.Format(
            RabbitMQConnection_InitializeConnection_ConnectionFailedAfterRetry,
            RetryCount));

        throw new InvalidOperationException(RabbitMQConnection_InitializeConnection_ConnectionFailedUnexpectedly);
    }
}
