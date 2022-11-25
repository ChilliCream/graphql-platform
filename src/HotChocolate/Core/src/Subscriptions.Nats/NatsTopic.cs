using System.Threading.Channels;
using AlterNats;

namespace HotChocolate.Subscriptions.Nats;

internal sealed class NatsTopic<TMessage> : DefaultTopic<NatsMessageEnvelope<TMessage>, TMessage>
{
    private readonly NatsConnection _connection;
    private readonly IMessageSerializer _serializer;

    public NatsTopic(
        string name,
        NatsConnection connection,
        IMessageSerializer serializer,
        int capacity,
        TopicBufferFullMode fullMode,
        ISubscriptionDiagnosticEvents diagnosticEvents)
        : base(name, capacity, fullMode, diagnosticEvents)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    protected override ValueTask<IDisposable> ConnectAsync(
        ChannelWriter<NatsMessageEnvelope<TMessage>> incoming)
        => _connection.SubscribeAsync(
            Name,
            async (string rawMessage) =>
            {
                DiagnosticEvents.Received(Name, rawMessage);
                var envelope = _serializer.Deserialize<NatsMessageEnvelope<TMessage>>(rawMessage);
                await incoming.WriteAsync(envelope).ConfigureAwait(false);
            });
}
