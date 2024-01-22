namespace HotChocolate.Subscriptions.Diagnostics;

/// <summary>
/// Inherit from this listener to implement a diagnostic listener for subscriptions.
/// </summary>
[DiagnosticEventSource(typeof(ISubscriptionDiagnosticEventsListener))]
public class SubscriptionDiagnosticEventsListener : ISubscriptionDiagnosticEventsListener
{
    /// <inheritdoc cref="ISubscriptionDiagnosticEvents.Created"/>
    public virtual void Created(string topicName) { }

    /// <inheritdoc cref="ISubscriptionDiagnosticEvents.Connected"/>
    public virtual void Connected(string topicName) { }

    /// <inheritdoc cref="ISubscriptionDiagnosticEvents.Disconnected"/>
    public virtual void Disconnected(string topicName) { }

    /// <inheritdoc cref="ISubscriptionDiagnosticEvents.MessageProcessingError"/>
    public virtual void MessageProcessingError(string topicName, Exception error) { }

    /// <inheritdoc cref="ISubscriptionDiagnosticEvents.Received"/>
    public virtual void Received(string topicName, string serializedMessage) { }

    /// <inheritdoc cref="ISubscriptionDiagnosticEvents.WaitForMessages"/>
    public virtual void WaitForMessages(string topicName) { }

    /// <inheritdoc cref="ISubscriptionDiagnosticEvents.Dispatch{T}"/>
    public virtual void Dispatch<T>(string topicName, T message, int subscribers) { }

    /// <inheritdoc cref="ISubscriptionDiagnosticEvents.TrySubscribe"/>
    public virtual void TrySubscribe(string topicName, int attempt) { }

    /// <inheritdoc cref="ISubscriptionDiagnosticEvents.SubscribeSuccess"/>
    public virtual void SubscribeSuccess(string topicName) { }

    /// <inheritdoc cref="ISubscriptionDiagnosticEvents.SubscribeFailed"/>
    public virtual void SubscribeFailed(string topicName) { }

    /// <inheritdoc cref="ISubscriptionDiagnosticEvents.Unsubscribe"/>
    public virtual void Unsubscribe(string topicName, int shard, int subscribers) { }

    /// <inheritdoc cref="ISubscriptionDiagnosticEvents.Close"/>
    public virtual void Close(string topicName) { }

    /// <inheritdoc cref="ISubscriptionDiagnosticEvents.Send{T}"/>
    public virtual void Send<T>(string topicName, T message) { }

    /// <inheritdoc cref="ISubscriptionDiagnosticEvents.ProviderInfo"/>
    public virtual void ProviderInfo(string infoText) { }

    /// <inheritdoc cref="ISubscriptionDiagnosticEvents.ProviderTopicInfo"/>
    public virtual void ProviderTopicInfo(string topicName, string infoText) { }
}
