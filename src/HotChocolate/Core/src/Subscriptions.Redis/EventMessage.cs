#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace HotChocolate.Subscriptions.Redis;

internal struct EventMessage<TBody>
{
    // we need this for serialization.
    public EventMessage()
    {
    }

    public EventMessage(TBody body)
    {
        Body = body;
        IsCompletedMessage = false;
    }

    // we need the setter for serialization.
    public TBody? Body { get; set; }

    // we need the setter for serialization.
#if NET6_0_OR_GREATER
    [MemberNotNullWhen(false, nameof(Body))]
#endif
    public bool IsCompletedMessage { get; set; }
}
