using System.Text;
using HotChocolate.Subscriptions.Diagnostics;
using RabbitMQ.Client;

namespace HotChocolate.Subscriptions.RabbitMQ;

internal sealed class RabbitMQPubSub : DefaultPubSub
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IRabbitMQConnection _connection;
    private readonly IMessageSerializer _serializer;
    private readonly RabbitMQSubscriptionOptions _rabbitMqSubscriptionOptions;
    private readonly string _completed;
    private readonly int _topicBufferCapacity;
    private readonly TopicBufferFullMode _topicBufferFullMode;
    private readonly BasicProperties _publishBasicProperties = new() { Persistent = true };

    public RabbitMQPubSub(
        IRabbitMQConnection connection,
        IMessageSerializer serializer,
        SubscriptionOptions options,
        RabbitMQSubscriptionOptions rabbitMqSubscriptionOptions,
        ISubscriptionDiagnosticEvents diagnosticEvents)
        : base(options, diagnosticEvents)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _rabbitMqSubscriptionOptions = rabbitMqSubscriptionOptions ?? throw new ArgumentNullException(nameof(rabbitMqSubscriptionOptions));
        _topicBufferCapacity = options.TopicBufferCapacity;
        _topicBufferFullMode = options.TopicBufferFullMode;
        _completed = serializer.CompleteMessage;
    }

    protected override async ValueTask OnSendAsync<TMessage>(
        string formattedTopic,
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        var serializedMessage = _serializer.Serialize(message);

        await PublishAsync(formattedTopic, serializedMessage, cancellationToken).ConfigureAwait(false);
    }

    protected override async ValueTask OnCompleteAsync(string formattedTopic)
        => await PublishAsync(formattedTopic, _completed, CancellationToken.None).ConfigureAwait(false);

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
            _rabbitMqSubscriptionOptions,
            DiagnosticEvents);

    private async Task PublishAsync(string formattedTopic, string message, CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetBytes(message);
        var channel = await _connection.GetChannelAsync(cancellationToken).ConfigureAwait(false);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await channel.BasicPublishAsync(
                exchange: formattedTopic,
                routingKey: string.Empty,
                mandatory: false,
                _publishBasicProperties,
                body,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            _semaphore.Dispose();
        }

        base.Dispose(disposing);
    }
}
