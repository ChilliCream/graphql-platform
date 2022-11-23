using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Subscriptions.InMemory;

internal sealed class EventMessageEnvelope<TBody>
{
    public EventMessageEnvelope()
    {
        Body = default;
        IsCompletedMessage = true;
    }

    public EventMessageEnvelope(TBody body)
    {
        Body = body;
        IsCompletedMessage = false;
    }

    public TBody? Body { get; }

    #if NET6_0_OR_GREATER
    [MemberNotNullWhen(false, nameof(Body))]
    #endif
    public bool IsCompletedMessage { get; }
}
