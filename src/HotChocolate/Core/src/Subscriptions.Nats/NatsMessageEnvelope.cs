namespace HotChocolate.Subscriptions.Nats;

internal sealed class NatsMessageEnvelope<TBody> : DefaultMessageEnvelope<TBody>
{
    public NatsMessageEnvelope(TBody? body = default, bool isCompletedMessage = true)
        : base(body, isCompletedMessage)
    {
    }
}
