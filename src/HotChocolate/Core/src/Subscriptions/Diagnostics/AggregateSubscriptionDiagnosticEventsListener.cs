namespace HotChocolate.Subscriptions.Diagnostics;

internal sealed class AggregateSubscriptionDiagnosticEventsListener
    : SubscriptionDiagnosticEventsListener
{
    private readonly ISubscriptionDiagnosticEventsListener[] _listeners;

    public AggregateSubscriptionDiagnosticEventsListener(
        ISubscriptionDiagnosticEventsListener[] listeners)
    {
        _listeners = listeners;
    }

    public override void Created(string topicName)
    {
        var listenerSpan = _listeners.AsSpan();

        for (var i = 0; i < listenerSpan.Length; i++)
        {
            listenerSpan[i].Created(topicName);
        }
    }

    public override void Connected(string topicName)
    {
        var listenerSpan = _listeners.AsSpan();

        for (var i = 0; i < listenerSpan.Length; i++)
        {
            listenerSpan[i].Connected(topicName);
        }
    }

    public override void Disconnected(string topicName)
    {
        var listenerSpan = _listeners.AsSpan();

        for (var i = 0; i < listenerSpan.Length; i++)
        {
            listenerSpan[i].Disconnected(topicName);
        }
    }

    public override void MessageProcessingError(string topicName, Exception error)
    {
        var listenerSpan = _listeners.AsSpan();

        for (var i = 0; i < listenerSpan.Length; i++)
        {
            listenerSpan[i].MessageProcessingError(topicName, error);
        }
    }

    public override void Received(string topicName, string serializedMessage)
    {
        var listenerSpan = _listeners.AsSpan();

        for (var i = 0; i < listenerSpan.Length; i++)
        {
            listenerSpan[i].Received(topicName, serializedMessage);
        }
    }

    public override void WaitForMessages(string topicName)
    {
        var listenerSpan = _listeners.AsSpan();

        for (var i = 0; i < listenerSpan.Length; i++)
        {
            listenerSpan[i].WaitForMessages(topicName);
        }
    }

    public override void Dispatch<T>(string topicName, T message, int subscribers)
    {
        var listenerSpan = _listeners.AsSpan();

        for (var i = 0; i < listenerSpan.Length; i++)
        {
            listenerSpan[i].Dispatch(topicName, message, subscribers);
        }
    }

    public override void TrySubscribe(string topicName, int attempt)
    {
        var listenerSpan = _listeners.AsSpan();

        for (var i = 0; i < listenerSpan.Length; i++)
        {
            listenerSpan[i].TrySubscribe(topicName, attempt);
        }
    }

    public override void SubscribeSuccess(string topicName)
    {
        var listenerSpan = _listeners.AsSpan();

        for (var i = 0; i < listenerSpan.Length; i++)
        {
            listenerSpan[i].SubscribeSuccess(topicName);
        }
    }

    public override void SubscribeFailed(string topicName)
    {
        var listenerSpan = _listeners.AsSpan();

        for (var i = 0; i < listenerSpan.Length; i++)
        {
            listenerSpan[i].SubscribeFailed(topicName);
        }
    }

    public override void Unsubscribe(string topicName, int shard, int subscribers)
    {
        var listenerSpan = _listeners.AsSpan();

        for (var i = 0; i < listenerSpan.Length; i++)
        {
            listenerSpan[i].Unsubscribe(topicName, shard, subscribers);
        }
    }

    public override void Close(string topicName)
    {
        var listenerSpan = _listeners.AsSpan();

        for (var i = 0; i < listenerSpan.Length; i++)
        {
            listenerSpan[i].Close(topicName);
        }
    }

    public override void Send<T>(string topicName, T message)
    {
        var listenerSpan = _listeners.AsSpan();

        for (var i = 0; i < listenerSpan.Length; i++)
        {
            listenerSpan[i].Send(topicName, message);
        }
    }

    public override void ProviderInfo(string infoText)
    {
        var listenerSpan = _listeners.AsSpan();

        for (var i = 0; i < listenerSpan.Length; i++)
        {
            listenerSpan[i].ProviderInfo(infoText);
        }
    }

    public override void ProviderTopicInfo(string topicName, string infoText)
    {
        var listenerSpan = _listeners.AsSpan();

        for (var i = 0; i < listenerSpan.Length; i++)
        {
            listenerSpan[i].ProviderTopicInfo(topicName, infoText);
        }
    }
}
