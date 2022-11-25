namespace HotChocolate.Subscriptions;

public sealed class MessageEnvelope<TBody>
{
    public MessageEnvelope(TBody? body = default, MessageKind kind = MessageKind.Default)
    {
        if (kind is MessageKind.Default && body is null)
        {
            throw new ArgumentNullException(nameof(body));
        }

        Body = body;
        Kind = kind;
    }

    public TBody? Body { get; }

    public MessageKind Kind { get; }
}
