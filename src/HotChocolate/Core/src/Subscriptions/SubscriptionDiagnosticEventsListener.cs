namespace HotChocolate.Subscriptions;

public class SubscriptionDiagnosticEventsListener : ISubscriptionDiagnosticEventsListener
{
    public virtual void Created(string topic)
    {
    }

    public virtual void Connected(string topic)
    {
    }

    public virtual void Disconnected(string topic)
    {
    }

    public virtual void MessageProcessingError(string topic, Exception ex)
    {
    }

    public virtual void Received(string topic, string message)
    {
    }

    public virtual void WaitForMessages(string topic)
    {
    }

    public virtual void Dispatched<T>(
        string topic,
        MessageEnvelope<T> message,
        int subscribers)
    {
    }

    public virtual void Delayed<T>(
        string topic,
        MessageEnvelope<T> message,
        int subscribers)
    {
    }

    public virtual void TrySubscribe(string topic, int attempt)
    {
    }

    public virtual void SubscribeSuccess(string topic)
    {
    }

    public virtual void SubscribeFailed(string topic)
    {
    }

    public virtual void Unsubscribe(string topic, int subscribers)
    {
    }

    public virtual void Close(string topic)
    {
    }

    public virtual void Send<T>(string topic, MessageEnvelope<T> message)
    {
    }

    public virtual  void ProviderInfo(string infoText)
    {
    }

    public virtual void ProviderTopicInfo(string topic, string infoText)
    {
    }
}
