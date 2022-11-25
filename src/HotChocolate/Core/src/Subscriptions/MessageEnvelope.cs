using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Subscriptions;

public sealed class MessageEnvelope<TBody>
{
    public MessageEnvelope(TBody? body = default, bool isCompletedMessage = false)
    {
        Body = body;
        IsCompletedMessage = isCompletedMessage;
    }

    public TBody? Body { get; }

    [MemberNotNullWhen(false, nameof(Body))]
    public bool IsCompletedMessage { get; }
}
