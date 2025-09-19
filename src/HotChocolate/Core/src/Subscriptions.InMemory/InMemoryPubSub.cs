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

    protected override ValueTask OnSendAsync<TMessage>(
        string formattedTopic,
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        if (TryGetTopic<TMessage>(formattedTopic, out var topic))
        {
            topic.Publish(message);
        }

        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnCompleteAsync(string formattedTopic)
    {
        if (TryGetTopic(formattedTopic, out var topic))
        {
            topic.Complete();
        }

        return ValueTask.CompletedTask;
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
