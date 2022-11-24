
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Subscriptions;

public abstract class DefaultMessageEnvelope<TBody>
{
    protected DefaultMessageEnvelope(TBody? body, bool isCompletedMessage)
    {
        Body = body;
        IsCompletedMessage = isCompletedMessage;
    }

    protected DefaultMessageEnvelope(TBody body)
    {
        Body = body;
        IsCompletedMessage = false;
    }

    protected DefaultMessageEnvelope()
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
