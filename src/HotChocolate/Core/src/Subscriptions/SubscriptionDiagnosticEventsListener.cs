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
        DefaultMessageEnvelope<T> message,
        int subscribers)
    {
    }

    public virtual void Delayed<T>(
        string topic,
        DefaultMessageEnvelope<T> message,
        int subscribers)
    {
    }

    public virtual void Subscribe(string topic)
    {
    }

    public virtual void Unsubscribe(string topic, int subscribers)
    {
    }

    public virtual void Close(string topic)
    {
    }

    public virtual void Send<T>(string topic, DefaultMessageEnvelope<T> message)
    {
    }
}
