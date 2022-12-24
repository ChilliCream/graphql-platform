using System.Text;
using HotChocolate.Subscriptions.Diagnostics;

namespace HotChocolate.Subscriptions.RabbitMQ;

internal sealed class RabbitMQPubSub : DefaultPubSub
{
    private readonly IRabbitMQConnection _connection;
    private readonly IMessageSerializer _serializer;
    private readonly string _completed;
    private readonly int _topicBufferCapacity;
    private readonly TopicBufferFullMode _topicBufferFullMode;

    public RabbitMQPubSub(
        IRabbitMQConnection connection,
        IMessageSerializer serializer,
        SubscriptionOptions options,
        ISubscriptionDiagnosticEvents diagnosticEvents)
        : base(options, diagnosticEvents)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _topicBufferCapacity = options.TopicBufferCapacity;
        _topicBufferFullMode = options.TopicBufferFullMode;
        _completed = serializer.CompleteMessage;
    }

    protected override async ValueTask OnSendAsync<TMessage>(
        string formattedTopic,
        MessageEnvelope<TMessage> message,
        CancellationToken cancellationToken = default)
    {
        var serializedMessage = _serializer.Serialize(message);

        await PublishAsync(formattedTopic, serializedMessage).ConfigureAwait(false);
    }

    protected override async ValueTask OnCompleteAsync(string formattedTopic) =>
        await PublishAsync(formattedTopic, _completed).ConfigureAwait(false);

    protected override DefaultTopic<TMessage> OnCreateTopic<TMessage>(
        string formattedTopic,
        int? bufferCapacity,
        TopicBufferFullMode? bufferFullMode)
        => new RabbitMQTopic<TMessage>(
            formattedTopic,
            _connection,
            _serializer,
            bufferCapacity ?? _topicBufferCapacity,
            bufferFullMode ?? _topicBufferFullMode,
            DiagnosticEvents);

    private async Task PublishAsync(string formattedTopic, string message)
    {
        var channel = await _connection.GetChannelAsync().ConfigureAwait(false);
        var properties = channel.CreateBasicProperties();
        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(
            exchange: formattedTopic,
            routingKey: string.Empty,
            mandatory: false,
            properties,
            body);
    }
}
