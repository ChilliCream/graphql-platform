namespace HotChocolate.Subscriptions;

public sealed class MessageEnvelope<TBody>
{
    public MessageEnvelope(TBody? body = default, MessageKind kind = MessageKind.Default)
    {
        if (kind is MessageKind.Default && body is null)
        {
            // TODO : resources
            throw new ArgumentException(
                "Default messages must have a body.",
                nameof(body));
        }

        if(kind is not MessageKind.Default && body is not null)
        {
            // TODO : resources
            throw new ArgumentException(
                "Complete and Unsubscribe messages do not have a body.",
                nameof(body));
        }

        Body = body;
        Kind = kind;
    }

    public TBody? Body { get; }

    public MessageKind Kind { get; }
}
