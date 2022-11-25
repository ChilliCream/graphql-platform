namespace HotChocolate.Subscriptions;

/// <summary>
/// This interfaces specifies the DataLoader diagnostics events.
/// </summary>
public interface ISubscriptionDiagnosticEvents
{
    void Created(string topic);

    void Connected(string topic);

    void Disconnected(string topic);

    void MessageProcessingError(string topic, Exception ex);

    void Received(string topic, string message);

    void WaitForMessages(string topic);

    void Dispatched<T>(string topic, MessageEnvelope<T> message, int subscribers); // subscribers = the ones that will be dispatched to

    void Delayed<T>(string topic, MessageEnvelope<T> message, int subscribers); // subscribers = the ones that were delayed

    void TrySubscribe(string topic, int attempt);

    void SubscribeSuccess(string topic);

    void SubscribeFailed(string topic);

    void Unsubscribe(string topic, int subscribers); // subscribers = the ones that were unsubscribed

    void Close(string topic);

    void Send<T>(string topic, MessageEnvelope<T> message);

    void ProviderInfo(string infoText);

    void ProviderTopicInfo(string topic, string infoText);
}
