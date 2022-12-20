using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using RabbitMQ.Client;
using HotChocolate.Subscriptions.Diagnostics;
using RabbitMQ.Client.Events;
using static HotChocolate.Subscriptions.RabbitMQ.RabbitMQResources;

namespace HotChocolate.Subscriptions.RabbitMQ;

internal sealed class RabbitMQTopic<TMessage> : DefaultTopic<TMessage>
{
    private readonly IRabbitMQConnection _connection;
    private readonly IMessageSerializer _serializer;

    public RabbitMQTopic(
        string name,
        IRabbitMQConnection connection,
        IMessageSerializer serializer,
        int capacity,
        TopicBufferFullMode fullMode,
        ISubscriptionDiagnosticEvents diagnosticEvents)
        : base(name, capacity, fullMode, diagnosticEvents)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    protected override async ValueTask<IDisposable> OnConnectAsync(
        ChannelWriter<MessageEnvelope<TMessage>> incoming,
        CancellationToken cancellationToken)
    {
        // We ensure that the processing is not started before the context is fully initialized.
        Debug.Assert(_connection != null, "_connection != null");
        Debug.Assert(_connection != null, "_serializer != null");

        var channel = await _connection.GetChannelAsync().ConfigureAwait(false);
        var queueName = Guid.NewGuid().ToString();
        var consumer = CreateConsumer(channel, queueName);

        async Task Received(object sender, BasicDeliverEventArgs args)
        {
            try
            {
                var serializedMessage = Encoding.UTF8.GetString(args.Body.Span);

                await DispatchAsync(incoming, serializedMessage).ConfigureAwait(false);
            }
            finally
            {
                channel.BasicAck(args.DeliveryTag, false);
            }
        }

        consumer.Received += Received;

        var consumerTag = channel.BasicConsume(consumer, queueName);

        DiagnosticEvents.ProviderTopicInfo(Name, RabbitMQTopic_ConnectAsync_SubscribedToRabbitMQ);

        return new Subscription(() =>
        {
            channel.BasicCancelNoWait(consumerTag);
            consumer.Received -= Received;
            DiagnosticEvents.ProviderTopicInfo(Name, Subscription_Unsubscribe_UnsubscribedFromRabbitMQ);
        });
    }

    private async ValueTask DispatchAsync(
        ChannelWriter<MessageEnvelope<TMessage>> incoming,
        string serializedMessage)
    {
        // we ensure that if there is noise on the channel we filter it out.
        if (!string.IsNullOrEmpty(serializedMessage))
        {
            DiagnosticEvents.Received(Name, serializedMessage);

            var envelope = _serializer.Deserialize<MessageEnvelope<TMessage>>(serializedMessage);

            await incoming.WriteAsync(envelope).ConfigureAwait(false);
        }
    }

    private AsyncEventingBasicConsumer CreateConsumer(IModel channel, string queueName)
    {
        channel.ExchangeDeclare(
            exchange: Name,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false);
        channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: true,
            autoDelete: false);
        channel.QueueBind(
            exchange: Name,
            queue: queueName,
            routingKey: string.Empty);

        return new AsyncEventingBasicConsumer(channel);
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action _unsubscribe;
        private bool _disposed;

        public Subscription(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _unsubscribe();
            _disposed = true;
        }
    }
}
