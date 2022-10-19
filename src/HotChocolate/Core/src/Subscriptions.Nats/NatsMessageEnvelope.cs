using MessagePack;

namespace HotChocolate.Subscriptions.Nats;

[MessagePackObject(true)]
public sealed class NatsMessageEnvelope<TBody>
{
    public NatsMessageEnvelope(TBody? body, NatsMessageType messageType = NatsMessageType.Message)
    {
        if (messageType == NatsMessageType.Message && body == null)
        {
            throw new ArgumentNullException(nameof(body));
        }
        MessageType = messageType;
        Body = body;
    }

    public NatsMessageType MessageType { get; }
    public TBody? Body { get; }

    public static NatsMessageEnvelope<TBody> Completed { get; } = new(default, NatsMessageType.Completed);
}
