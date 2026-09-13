using Mocha.Middlewares;
using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats;

/// <summary>
/// Fluent descriptor for a receive endpoint backed by a durable JetStream consumer.
/// </summary>
public interface INatsReceiveEndpointDescriptor
    : IReceiveEndpointDescriptor<NatsReceiveEndpointConfiguration>
{
    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Handler{THandler}" />
    new INatsReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler;

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Handler(Type)" />
    new INatsReceiveEndpointDescriptor Handler(Type handlerType);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Consumer{TConsumer}" />
    new INatsReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Consumer(Type)" />
    new INatsReceiveEndpointDescriptor Consumer(Type consumerType);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Receives{TMessage}" />
    new INatsReceiveEndpointDescriptor Receives<TMessage>();

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Receives(Type)" />
    new INatsReceiveEndpointDescriptor Receives(Type messageType);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.MaxConcurrency" />
    new INatsReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency);

    /// <summary>
    /// Reads from a specific stream instead of resolving the owning stream at start-up.
    /// </summary>
    /// <param name="streamName">The stream name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsReceiveEndpointDescriptor FromStream(string streamName);

    /// <summary>
    /// Sets the durable consumer name, which defaults to the sanitized endpoint name.
    /// </summary>
    /// <param name="consumerName">The durable consumer name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsReceiveEndpointDescriptor ConsumerName(string consumerName);

    /// <summary>
    /// Adds a subject this endpoint receives, in addition to any derived from its handlers.
    /// </summary>
    /// <param name="subject">The subject or wildcard filter.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsReceiveEndpointDescriptor Subject(string subject);

    /// <summary>
    /// Sets how long the server waits for an acknowledgement before redelivering.
    /// </summary>
    /// <param name="ackWait">The acknowledgement deadline.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsReceiveEndpointDescriptor AckWait(TimeSpan ackWait);

    /// <summary>
    /// Sets how many times a message is delivered before it is treated as undeliverable.
    /// </summary>
    /// <param name="maxDeliver">The delivery limit.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsReceiveEndpointDescriptor MaxDeliver(long maxDeliver);

    /// <summary>
    /// Sets the delays applied before each redelivery.
    /// </summary>
    /// <param name="backoff">The delays, one per redelivery attempt.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsReceiveEndpointDescriptor Backoff(params TimeSpan[] backoff);

    /// <summary>
    /// Sets the maximum number of unacknowledged messages in flight across every instance reading
    /// this endpoint's consumer.
    /// </summary>
    /// <param name="maxAckPending">The ceiling.</param>
    /// <returns>The descriptor for method chaining.</returns>
    /// <remarks>
    /// Distinct from <c>MaxConcurrency</c>, which bounds one instance. Lowering this to a single
    /// instance's concurrency starves the other instances reading the same consumer.
    /// </remarks>
    INatsReceiveEndpointDescriptor MaxAckPending(long maxAckPending);

    /// <summary>
    /// Reports progress on an in-flight message at the specified interval, extending its
    /// acknowledgement deadline for as long as the handler is still running.
    /// </summary>
    /// <param name="interval">How often to report progress.</param>
    /// <returns>The descriptor for method chaining.</returns>
    /// <remarks>
    /// Off by default, because it costs a background task per in-flight message.
    /// </remarks>
    INatsReceiveEndpointDescriptor AckProgressEvery(TimeSpan interval);

    /// <summary>
    /// Sets where the consumer starts reading when it is created, which has no effect on a consumer
    /// that already exists.
    /// </summary>
    /// <param name="deliverPolicy">The delivery policy.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsReceiveEndpointDescriptor DeliverFrom(ConsumerConfigDeliverPolicy deliverPolicy);

    /// <summary>
    /// Sets the address failed messages are forwarded to, replacing the one derived from the
    /// endpoint name.
    /// </summary>
    /// <param name="address">The fault endpoint address, for example <c>nats:s/orders_error</c>.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsReceiveEndpointDescriptor FaultEndpoint(Uri address);

    /// <summary>
    /// Stops failed messages being forwarded to a fault endpoint.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    INatsReceiveEndpointDescriptor DisableFaultEndpoint();

    /// <summary>
    /// Sets the address skipped messages are forwarded to, replacing the one derived from the
    /// endpoint name.
    /// </summary>
    /// <param name="address">The skipped endpoint address.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsReceiveEndpointDescriptor SkippedEndpoint(Uri address);

    /// <summary>
    /// Stops skipped messages being forwarded to a skipped endpoint.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    INatsReceiveEndpointDescriptor DisableSkippedEndpoint();

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.UseReceive" />
    new INatsReceiveEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);
}
