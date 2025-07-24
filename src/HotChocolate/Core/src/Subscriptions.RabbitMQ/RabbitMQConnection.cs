using HotChocolate.Subscriptions.Diagnostics;
using HotChocolate.Utilities;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using static HotChocolate.Subscriptions.RabbitMQ.RabbitMQResources;

namespace HotChocolate.Subscriptions.RabbitMQ;

internal sealed class RabbitMQConnection : IRabbitMQConnection, IDisposable
{
    private const int RetryCount = 6;
    private readonly object _sync = new();
    private readonly TaskCompletionSource<IConnection> _completionSource = new();
    private readonly ISubscriptionDiagnosticEvents _diagnosticEvents;
    private bool _disposed;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMQConnection(ISubscriptionDiagnosticEvents diagnosticEvents, ConnectionFactory connectionFactory)
    {
        _diagnosticEvents = diagnosticEvents ?? throw new ArgumentNullException(nameof(diagnosticEvents));

        ArgumentNullException.ThrowIfNull(connectionFactory);

        InitializeConnection(connectionFactory);
    }

    public async Task<IModel> GetChannelAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_channel is not null)
        {
            return _channel;
        }

        var connection = await _completionSource.Task.ConfigureAwait(false);

        lock (_sync)
        {
            var channel = connection.CreateModel();

            channel.CallbackException +=
                (_, args) => _diagnosticEvents.ProviderInfo(args.Exception.Message);

            _connection = connection;

            return _channel ??= channel;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _completionSource.TrySetCanceled();
        _channel?.Dispose();
        _connection?.Dispose();
    }

    private void InitializeConnection(ConnectionFactory connectionFactory)
        => InitializeConnectionAsync(connectionFactory).FireAndForget();

    private async Task InitializeConnectionAsync(ConnectionFactory connectionFactory)
    {
        connectionFactory.AutomaticRecoveryEnabled = true;
        connectionFactory.DispatchConsumersAsync = true;
        var connectionAttempt = 0;

        while (connectionAttempt < RetryCount)
        {
            try
            {
                var connection = connectionFactory.CreateConnection();

                if (_completionSource.TrySetResult(connection))
                {
                    return;
                }

                throw new InvalidOperationException(
                    RabbitMQConnection_InitializeConnection_ConnectionSucceededButFailedUnexpectedly);
            }
            catch (BrokerUnreachableException)
            {
                connectionAttempt++;
                _diagnosticEvents.ProviderInfo(string.Format(
                    RabbitMQConnection_InitializeConnection_ConnectionAttemptFailed,
                    connectionAttempt));
            }

            if (connectionAttempt < RetryCount)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, connectionAttempt))).ConfigureAwait(false);
            }
        }

        _diagnosticEvents.ProviderInfo(string.Format(
            RabbitMQConnection_InitializeConnection_ConnectionFailedAfterRetry,
            connectionAttempt));

        if (!_completionSource.TrySetException(new RabbitMQConnectionFailedException(connectionAttempt)))
        {
            throw new InvalidOperationException(RabbitMQConnection_InitializeConnection_ConnectionFailedUnexpectedly);
        }
    }
}
