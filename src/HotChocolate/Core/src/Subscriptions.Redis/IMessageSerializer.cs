namespace HotChocolate.Subscriptions.Redis
{
    /// <summary>
    /// The redis event message serializer.
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        /// Serializes a topic ot a message to a string
        /// that is used to send it to the redis pub/sub.
        /// </summary>
        /// <param name="message">The message that shall be serialized.</param>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <returns>
        /// Returns a string representing the serialized message.
        /// </returns>
        string Serialize<TMessage>(TMessage message);

        /// <summary>
        /// Deserializes a topic ot a message from a string to <typeparamref name="TMessage"/>.
        /// </summary>
        /// <param name="serializedMessage">
        /// The serialized message that shall be deserialized.
        /// </param>
        /// <typeparam name="TMessage">The type of the deserialized message.</typeparam>
        /// <returns>
        /// Returns the deserialized message object.
        /// </returns>
        TMessage Deserialize<TMessage>(string serializedMessage);
    }
}
