using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using static System.StringComparer;
using static HotChocolate.Subscriptions.Properties.Resources;

namespace HotChocolate.Subscriptions;

public abstract class DefaultPubSub : ITopicEventReceiver, ITopicEventSender
{
    private readonly ConcurrentDictionary<string, IDisposable> _topics = new(Ordinal);
    private readonly TopicFormatter _topicFormatter;
    private readonly ISubscriptionDiagnosticEvents _diagnosticEvents;
    private readonly MessageEnvelope<object> _completed = new(isCompletedMessage: true);

    protected DefaultPubSub(
        SubscriptionOptions options,
        ISubscriptionDiagnosticEvents diagnosticEvents)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (diagnosticEvents is null)
        {
            throw new ArgumentNullException(nameof(diagnosticEvents));
        }

        _topicFormatter = new TopicFormatter(options.TopicPrefix);
        _diagnosticEvents = diagnosticEvents;
    }
    
    protected ISubscriptionDiagnosticEvents DiagnosticEvents => _diagnosticEvents;

    public async ValueTask<ISourceStream<TMessage>> SubscribeAsync<TMessage>(
        string topic,
        int? bufferCapacity = null,
        TopicBufferFullMode? bufferFullMode = null,
        CancellationToken cancellationToken = default)
    {
        if (topic is null)
        {
            throw new ArgumentNullException(nameof(topic));
        }

        if (bufferCapacity < 8)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bufferCapacity),
                bufferCapacity,
                DefaultPubSub_SubscribeAsync_MinimumAllowedBufferSize);
        }

        var formattedTopic = FormatTopicName(topic);
        ISourceStream<TMessage>? sourceStream = null;
        const int allowedAttempts = 4;
        var attempt = 0;

        while (allowedAttempts > attempt && sourceStream is null)
        {
            attempt++;

            if (attempt > 1)
            {
                await Task.Delay(25 * attempt, cancellationToken);
            }

            _diagnosticEvents.TrySubscribe(formattedTopic, attempt);

            var eventTopic = _topics.GetOrAdd(
                formattedTopic,
                t => CreateTopic<TMessage>(t, bufferCapacity, bufferFullMode));

            if (eventTopic is DefaultTopic<TMessage> et)
            {
                sourceStream = await et.TrySubscribeAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // we found a topic with the same name but a different message type.
                // this is an invalid state and we will except.
                throw new InvalidMessageTypeException();
            }
        }

        if (sourceStream is null)
        {
            _diagnosticEvents.SubscribeFailed(formattedTopic);
            throw new CannotSubscribeException();
        }

        return sourceStream;
    }

    public ValueTask SendAsync<TMessage>(
        string topic,
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        if (topic is null)
        {
            throw new ArgumentNullException(nameof(topic));
        }

        var formattedTopic = FormatTopicName(topic);
        var envelopedMessage = new MessageEnvelope<TMessage>(message);
        _diagnosticEvents.Send(formattedTopic, envelopedMessage);

        return OnSendAsync(formattedTopic, envelopedMessage, cancellationToken);
    }

    protected abstract ValueTask OnSendAsync<TMessage>(
        string formattedTopic,
        MessageEnvelope<TMessage> message,
        CancellationToken cancellationToken = default);

    public ValueTask CompleteAsync(string topic)
    {
        if (topic is null)
        {
            throw new ArgumentNullException(nameof(topic));
        }

        var formattedTopic = _topicFormatter.Format(topic);
        _diagnosticEvents.Send(formattedTopic, _completed);

        return OnCompleteAsync(formattedTopic);
    }

    protected abstract ValueTask OnCompleteAsync(
        string formattedTopic);

    private DefaultTopic<TMessage> CreateTopic<TMessage>(
        string formattedTopic,
        int? bufferCapacity,
        TopicBufferFullMode? bufferFullMode)
    {
        var eventTopic = OnCreateTopic<TMessage>(formattedTopic, bufferCapacity, bufferFullMode);

        eventTopic.Unsubscribed += (sender, __) =>
        {
            var s = (DefaultTopic<TMessage>)sender!;
            _topics.TryRemove(s.Name, out _);
            s.Dispose();
        };

        return eventTopic;
    }

    protected abstract DefaultTopic<TMessage> OnCreateTopic<TMessage>(
        string formattedTopic,
        int? bufferCapacity,
        TopicBufferFullMode? bufferFullMode);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual string FormatTopicName(string topic)
        => _topicFormatter.Format(topic);

    protected bool TryGetTopic<TTopic>(
        string formattedTopic,
        [NotNullWhen(true)] out TTopic? topic)
    {
        if (_topics.TryGetValue(formattedTopic, out var value))
        {
            if (value is TTopic casted)
            {
                topic = casted;
                return true;
            }

            throw new InvalidMessageTypeException();
        }

        topic = default;
        return false;
    }
}
