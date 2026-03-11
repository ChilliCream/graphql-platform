using System.Diagnostics;
using System.Text;
using RabbitMQ.Client;
using HotChocolate.Subscriptions.Diagnostics;
using RabbitMQ.Client.Events;
using static HotChocolate.Subscriptions.RabbitMQ.RabbitMQResources;

namespace HotChocolate.Subscriptions.RabbitMQ;

internal sealed class RabbitMQTopic<TMessage> : DefaultTopic<TMessage>
{
    private readonly IRabbitMQConnection _connection;
    private readonly IMessageSerializer _serializer;
    private readonly RabbitMQSubscriptionOptions _rabbitMqSubscriptionOptions;

    public RabbitMQTopic(
        string name,
        IRabbitMQConnection connection,
        IMessageSerializer serializer,
        int capacity,
        TopicBufferFullMode fullMode,
        RabbitMQSubscriptionOptions rabbitMqSubscriptionOptions,
        ISubscriptionDiagnosticEvents diagnosticEvents)
        : base(name, capacity, fullMode, diagnosticEvents)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _rabbitMqSubscriptionOptions = rabbitMqSubscriptionOptions ?? throw new ArgumentNullException(nameof(rabbitMqSubscriptionOptions));
    }

    protected override async ValueTask<IAsyncDisposable> OnConnectAsync(CancellationToken cancellationToken)
    {
        // We ensure that the processing is not started before the context is fully initialized.
        Debug.Assert(_connection != null);
        Debug.Assert(_serializer != null);

        var channel = await _connection.GetChannelAsync(cancellationToken).ConfigureAwait(false);

        var queueName = string.IsNullOrEmpty(_rabbitMqSubscriptionOptions.QueuePrefix)
            ? string.Empty // use server-generated name
            : _rabbitMqSubscriptionOptions.QueuePrefix + Guid.NewGuid();

        await channel.ExchangeDeclareAsync(
            exchange: Name,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: true,
            autoDelete: true,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queue: queueName,
            exchange: Name,
            routingKey: string.Empty,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                var serializedMessage = Encoding.UTF8.GetString(args.Body.Span);
                DispatchMessage(_serializer, serializedMessage);
            }
            finally
            {
                await channel.BasicAckAsync(deliveryTag: args.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            }
        };

        await channel.BasicConsumeAsync(
            queueName,
            autoAck: false,
            consumerTag: string.Empty,
            noLocal: false,
            exclusive: true,
            arguments: null,
            consumer,
            cancellationToken);

        DiagnosticEvents.ProviderTopicInfo(Name, RabbitMQTopic_ConnectAsync_SubscribedToRabbitMQ);

        return new Subscription(async () =>
        {
            await channel.BasicCancelAsync(consumer.ConsumerTags.First(), noWait: false, cancellationToken);
            DiagnosticEvents.ProviderTopicInfo(Name, Subscription_UnsubscribedFromRabbitMQ);
        });
    }

    private sealed class Subscription : IAsyncDisposable
    {
        private readonly Func<Task> _unsubscribe;
        private bool _disposed;

        public Subscription(Func<Task> unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            await _unsubscribe();
            _disposed = true;
        }
    }
}
