namespace Mocha.Transport.NATS;

/// <summary>
/// Fluent interface for configuring a NATS JetStream stream.
/// </summary>
public interface INatsStreamDescriptor : IMessagingDescriptor<NatsStreamConfiguration>
{
    /// <summary>
    /// Sets the name of the stream.
    /// </summary>
    /// <param name="name">The stream name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsStreamDescriptor Name(string name);

    /// <summary>
    /// Adds a subject that this stream captures.
    /// </summary>
    /// <param name="subject">The subject pattern (e.g., "orders.>" or "orders.created").</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsStreamDescriptor Subject(string subject);

    /// <summary>
    /// Sets the maximum number of messages in the stream.
    /// </summary>
    /// <param name="maxMsgs">The maximum message count.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsStreamDescriptor MaxMsgs(long maxMsgs);

    /// <summary>
    /// Sets the maximum total size in bytes of the stream.
    /// </summary>
    /// <param name="maxBytes">The maximum byte size.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsStreamDescriptor MaxBytes(long maxBytes);

    /// <summary>
    /// Sets the maximum age of messages in the stream.
    /// </summary>
    /// <param name="maxAge">The maximum message age.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsStreamDescriptor MaxAge(TimeSpan maxAge);

    /// <summary>
    /// Sets the number of replicas for this stream in a NATS JetStream cluster.
    /// </summary>
    /// <param name="replicas">The number of replicas.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsStreamDescriptor Replicas(int replicas);

    /// <summary>
    /// Sets whether the stream should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision">True to enable auto-provisioning (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsStreamDescriptor AutoProvision(bool autoProvision = true);
}
