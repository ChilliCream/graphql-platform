namespace Mocha;

/// <summary>
/// Provides the primary API for dispatching messages through the message bus, supporting publish, send, request-reply, and reply patterns.
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publishes a message to all subscribers of the specified message type.
    /// </summary>
    /// <typeparam name="T">The type of the message to publish.</typeparam>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="cancellationToken">A token to cancel the publish operation.</param>
    /// <returns>A task that completes when the message has been handed off to the transport.</returns>
    ValueTask PublishAsync<T>(T message, CancellationToken cancellationToken);

    /// <summary>
    /// Publishes a message to all subscribers of the specified message type with additional publish options.
    /// </summary>
    /// <typeparam name="T">The type of the message to publish.</typeparam>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="options">Options controlling publish behavior such as headers and expiration.</param>
    /// <param name="cancellationToken">A token to cancel the publish operation.</param>
    /// <returns>A task that completes when the message has been handed off to the transport.</returns>
    ValueTask PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a message to a single receiver determined by the message type's routing configuration.
    /// </summary>
    /// <param name="message">The message instance to send.</param>
    /// <param name="cancellationToken">A token to cancel the send operation.</param>
    /// <returns>A task that completes when the message has been handed off to the transport.</returns>
    ValueTask SendAsync(object message, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a message to a single receiver with additional send options.
    /// </summary>
    /// <param name="message">The message instance to send.</param>
    /// <param name="options">Options controlling send behavior such as headers and expiration.</param>
    /// <param name="cancellationToken">A token to cancel the send operation.</param>
    /// <returns>A task that completes when the message has been handed off to the transport.</returns>
    ValueTask SendAsync(object message, SendOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a request message and waits for a typed response from the handler.
    /// </summary>
    /// <typeparam name="TResponse">The expected response event type.</typeparam>
    /// <param name="message">The request message to send.</param>
    /// <param name="cancellationToken">A token to cancel the request operation.</param>
    /// <returns>The response received from the handler.</returns>
    /// <exception cref="Events.ResponseTimeoutException">Thrown when no response is received within the configured timeout.</exception>
    ValueTask<TResponse> RequestAsync<TResponse>(IEventRequest<TResponse> message, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a request message with additional send options and waits for a typed response from the handler.
    /// </summary>
    /// <typeparam name="TResponse">The expected response event type.</typeparam>
    /// <param name="message">The request message to send.</param>
    /// <param name="options">Options controlling send behavior such as headers and expiration.</param>
    /// <param name="cancellationToken">A token to cancel the request operation.</param>
    /// <returns>The response received from the handler.</returns>
    /// <exception cref="Events.ResponseTimeoutException">Thrown when no response is received within the configured timeout.</exception>
    ValueTask<TResponse> RequestAsync<TResponse>(
        IEventRequest<TResponse> message,
        SendOptions options,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sends a request message and waits for acknowledgment without a typed response payload.
    /// </summary>
    /// <param name="message">The request message to send.</param>
    /// <param name="cancellationToken">A token to cancel the request operation.</param>
    /// <returns>A task that completes when the acknowledgment is received.</returns>
    /// <exception cref="Events.ResponseTimeoutException">Thrown when no acknowledgment is received within the configured timeout.</exception>
    ValueTask RequestAsync(object message, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a request message with additional send options and waits for acknowledgment without a typed response payload.
    /// </summary>
    /// <param name="message">The request message to send.</param>
    /// <param name="options">Options controlling send behavior such as headers and expiration.</param>
    /// <param name="cancellationToken">A token to cancel the request operation.</param>
    /// <returns>A task that completes when the acknowledgment is received.</returns>
    /// <exception cref="Events.ResponseTimeoutException">Thrown when no acknowledgment is received within the configured timeout.</exception>
    ValueTask RequestAsync(object message, SendOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a reply message back to the original requester using the response address from the consume context.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response message.</typeparam>
    /// <param name="response">The response message to send back.</param>
    /// <param name="options">Options specifying the reply destination and correlation information.</param>
    /// <param name="cancellationToken">A token to cancel the reply operation.</param>
    /// <returns>A task that completes when the reply has been handed off to the transport.</returns>
    ValueTask ReplyAsync<TResponse>(TResponse response, ReplyOptions options, CancellationToken cancellationToken)
        where TResponse : notnull;

    /// <summary>
    /// Publishes a message scheduled for delivery at the specified time.
    /// </summary>
    /// <typeparam name="T">The type of the message to publish.</typeparam>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="scheduledTime">The absolute time at which the message should be delivered.</param>
    /// <param name="cancellationToken">A token to cancel the publish operation.</param>
    /// <returns>A scheduling result containing the cancellation token and metadata.</returns>
    ValueTask<SchedulingResult> SchedulePublishAsync<T>(
        T message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken) where T : notnull;

    /// <summary>
    /// Publishes a message scheduled for delivery at the specified time with additional options.
    /// </summary>
    /// <typeparam name="T">The type of the message to publish.</typeparam>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="scheduledTime">The absolute time at which the message should be delivered.</param>
    /// <param name="options">Options controlling publish behavior such as headers and expiration.</param>
    /// <param name="cancellationToken">A token to cancel the publish operation.</param>
    /// <returns>A scheduling result containing the cancellation token and metadata.</returns>
    ValueTask<SchedulingResult> SchedulePublishAsync<T>(
        T message,
        DateTimeOffset scheduledTime,
        PublishOptions options,
        CancellationToken cancellationToken) where T : notnull;

    /// <summary>
    /// Sends a message scheduled for delivery at the specified time.
    /// </summary>
    /// <param name="message">The message instance to send.</param>
    /// <param name="scheduledTime">The absolute time at which the message should be delivered.</param>
    /// <param name="cancellationToken">A token to cancel the send operation.</param>
    /// <returns>A scheduling result containing the cancellation token and metadata.</returns>
    ValueTask<SchedulingResult> ScheduleSendAsync(
        object message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sends a message scheduled for delivery at the specified time with additional options.
    /// </summary>
    /// <param name="message">The message instance to send.</param>
    /// <param name="scheduledTime">The absolute time at which the message should be delivered.</param>
    /// <param name="options">Options controlling send behavior such as headers and expiration.</param>
    /// <param name="cancellationToken">A token to cancel the send operation.</param>
    /// <returns>A scheduling result containing the cancellation token and metadata.</returns>
    ValueTask<SchedulingResult> ScheduleSendAsync(
        object message,
        DateTimeOffset scheduledTime,
        SendOptions options,
        CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a previously scheduled message. Returns <c>true</c> if the message was cancelled,
    /// <c>false</c> if it was already dispatched, already cancelled, or not found.
    /// </summary>
    /// <param name="token">The opaque scheduling token returned by a prior schedule operation.</param>
    /// <param name="cancellationToken">A token to cancel the cancellation operation.</param>
    /// <returns><c>true</c> if the scheduled message was cancelled; otherwise <c>false</c>.</returns>
    ValueTask<bool> CancelScheduledMessageAsync(
        string token,
        CancellationToken cancellationToken);
}
