using HotChocolate.Subscriptions.Diagnostics;

namespace HotChocolate.Subscriptions.InMemory;

/// <summary>
/// The in-memory pub/sub provider implementation.
/// </summary>
internal sealed class InMemoryPubSub : DefaultPubSub
{
    private readonly SubscriptionOptions _options;
    private readonly ISubscriptionDiagnosticEvents _diagnosticEvents;

    public InMemoryPubSub(
        SubscriptionOptions options,
        ISubscriptionDiagnosticEvents diagnosticEvents)
        : base(options, diagnosticEvents)
    {
        _options = options;
        _diagnosticEvents = diagnosticEvents;
    }

    protected override async ValueTask OnSendAsync<TMessage>(
        string formattedTopic,
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        if (TryGetTopic<InMemoryTopic<TMessage>>(formattedTopic, out var topic))
        {
            await topic.PublishAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    protected override DefaultTopic<TMessage> OnCreateTopic<TMessage>(
        string formattedTopic,
        int? bufferCapacity,
        TopicBufferFullMode? bufferFullMode)
        => new InMemoryTopic<TMessage>(
            formattedTopic,
            bufferCapacity ?? _options.TopicBufferCapacity,
            bufferFullMode ?? _options.TopicBufferFullMode,
            _diagnosticEvents);

    protected override string FormatTopicName(string topic)
        => topic;
}
