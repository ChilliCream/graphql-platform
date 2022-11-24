using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Subscriptions.InMemory;

internal sealed class InMemoryMessageEnvelope<TBody> : DefaultMessageEnvelope<TBody>
{
    public InMemoryMessageEnvelope(TBody body)
        : base(body)
    {
    }

    public InMemoryMessageEnvelope()
    {
    }
}

