namespace HotChocolate.Subscriptions.Redis;

internal sealed class RedisMessageEnvelope<TBody> : DefaultMessageEnvelope<TBody>
{
    public RedisMessageEnvelope(TBody? body = default, bool isCompletedMessage = true)
        : base(body, isCompletedMessage)
    {
    }
}
