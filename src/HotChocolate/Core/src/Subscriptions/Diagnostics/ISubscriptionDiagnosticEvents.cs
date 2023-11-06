using HotChocolate.Execution;

namespace HotChocolate.Subscriptions.Diagnostics;

/// <summary>
/// This interfaces specifies the subscription provider diagnostics events.
/// </summary>
public interface ISubscriptionDiagnosticEvents
{
    /// <summary>
    /// An event topic was created.
    /// </summary>
    /// <param name="topicName">
    /// The name of the topic.
    /// </param>
    void Created(string topicName);

    /// <summary>
    /// The event topic has connected to the pub/sub system and is now ready to receive
    /// event messages.
    /// </summary>
    /// <param name="topicName">
    /// The name of the topic.
    /// </param>
    void Connected(string topicName);

    /// <summary>
    /// The topic was closed and has disconnected from the pub/sub system.
    /// </summary>
    /// <param name="topicName">
    /// The name of the topic.
    /// </param>
    void Disconnected(string topicName);

    /// <summary>
    /// An error occured during message processing or while connecting to the pub/sub system.
    /// </summary>
    /// <param name="topicName">
    /// The name of the topic.
    /// </param>
    /// <param name="error">
    /// The error.
    /// </param>
    void MessageProcessingError(string topicName, Exception error);

    /// <summary>
    /// Received a message from the pub/sub system.
    /// </summary>
    /// <param name="topicName">
    /// The name of the topic.
    /// </param>
    /// <param name="serializedMessage">
    /// The serialized message.
    /// </param>
    void Received(string topicName, string serializedMessage);

    /// <summary>
    /// The message dispatcher is waiting for new messages to continue dispatching messages
    /// to the subscribed <see cref="ISourceStream"/>s.
    /// </summary>
    /// <param name="topicName">
    /// The name of the topic.
    /// </param>
    void WaitForMessages(string topicName);

    /// <summary>
    /// A message is being dispatched to the subscribing <see cref="ISourceStream"/>s.
    /// </summary>
    /// <param name="topicName">
    /// The name of the topic.
    /// </param>
    /// <param name="message">
    /// The message that shall be dispatched.
    /// </param>
    /// <param name="subscribers">
    /// The count of subscribers the <paramref name="message"/> shall be dispatched to.
    /// </param>
    /// <typeparam name="T">
    /// The message body type.
    /// </typeparam>
    void Dispatch<T>(string topicName, T message, int subscribers);

    /// <summary>
    /// The GraphQL execution engine is trying to subscribe to a topic.
    /// </summary>
    /// <param name="topicName">
    /// The name of the topic.
    /// </param>
    /// <param name="attempt">
    /// The subscribe attempt count.
    /// </param>
    void TrySubscribe(string topicName, int attempt);

    /// <summary>
    /// The GraphQL execution engine has successfully subscribed to a topic.
    /// </summary>
    /// <param name="topicName">
    /// The name of the topic.
    /// </param>
    void SubscribeSuccess(string topicName);

    /// <summary>
    /// The GraphQL execution engine failed to subscribe to a topic.
    /// </summary>
    /// <param name="topicName">
    /// The name of the topic.
    /// </param>
    void SubscribeFailed(string topicName);

    /// <summary>
    /// The GraphQL execution engine has unsubscribed form the topic.
    /// </summary>
    /// <param name="topicName">
    /// The name of the topic.
    /// </param>
    /// <param name="shard">
    /// The shard to which the subscribers belonged to.
    /// </param>
    /// <param name="subscribers">
    /// The amount of subscribers that have unsubscribed.
    /// </param>
    void Unsubscribe(string topicName, int shard, int subscribers);

    /// <summary>
    /// An event topic was closed.
    /// </summary>
    /// <param name="topicName">
    /// The name of the topic.
    /// </param>
    void Close(string topicName);

    /// <summary>
    /// An event message was send to the pub/sub system. (outgoing message)
    /// </summary>
    /// <param name="topicName">
    /// The name of the topic.
    /// </param>
    /// <param name="message">
    /// The outgoing message.
    /// </param>
    /// <typeparam name="T">
    /// The message body type.
    /// </typeparam>
    void Send<T>(string topicName, T message);

    /// <summary>
    /// Provider specific information.
    /// </summary>
    /// <param name="infoText">
    /// The information text.
    /// </param>
    void ProviderInfo(string infoText);

    /// <summary>
    /// Provider specific information with a topic context.
    /// </summary>
    /// <param name="topicName">
    /// The name of the topic.
    /// </param>
    /// <param name="infoText">
    /// The information text.
    /// </param>
    void ProviderTopicInfo(string topicName, string infoText);
}
