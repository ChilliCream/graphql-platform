#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using MessagePack;

namespace HotChocolate.Subscriptions.Nats;

[MessagePackObject(true)]
internal sealed class EventMessageEnvelope<TBody>
{
    [SerializationConstructor]
    private EventMessageEnvelope(TBody? body, bool isCompletedMessage)
    {
        Body = body;
        IsCompletedMessage = isCompletedMessage;
    }

    public EventMessageEnvelope(TBody body)
    {
        Body = body;
        IsCompletedMessage = false;
    }

    public EventMessageEnvelope()
    {
        Body = default;
        IsCompletedMessage = true;
    }

    public TBody? Body { get; }

#if NET6_0_OR_GREATER
    [MemberNotNullWhen(false, nameof(Body))]
#endif
    public bool IsCompletedMessage { get; }
}
