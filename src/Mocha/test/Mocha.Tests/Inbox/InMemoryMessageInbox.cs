using System.Collections.Concurrent;
using Mocha.Inbox;
using Mocha.Middlewares;

namespace Mocha.Tests.Inbox;

internal sealed class InMemoryMessageInbox : IMessageInbox
{
    private readonly ConcurrentDictionary<(string MessageId, string ConsumerType), MessageEnvelope> _processed = new();

    public ConcurrentBag<MessageEnvelope> RecordedEnvelopes { get; } = [];

    public ValueTask<bool> ExistsAsync(
        string messageId,
        string consumerType,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(_processed.ContainsKey((messageId, consumerType)));
    }

    public ValueTask<bool> TryClaimAsync(
        MessageEnvelope envelope,
        string consumerType,
        CancellationToken cancellationToken)
    {
        if (envelope.MessageId is null)
        {
            return ValueTask.FromResult(false);
        }

        var claimed = _processed.TryAdd((envelope.MessageId, consumerType), envelope);
        if (claimed)
        {
            RecordedEnvelopes.Add(envelope);
        }

        return ValueTask.FromResult(claimed);
    }

    public ValueTask RecordAsync(
        MessageEnvelope envelope,
        string consumerType,
        CancellationToken cancellationToken)
    {
        if (envelope.MessageId is not null)
        {
            _processed.TryAdd((envelope.MessageId, consumerType), envelope);
        }

        RecordedEnvelopes.Add(envelope);
        return ValueTask.CompletedTask;
    }

    public ValueTask<int> CleanupAsync(
        TimeSpan maxAge,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(0);
    }
}
