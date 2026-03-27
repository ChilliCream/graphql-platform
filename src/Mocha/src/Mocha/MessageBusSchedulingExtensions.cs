namespace Mocha;

/// <summary>
/// Provides convenience extension methods on <see cref="IMessageBus"/> for scheduling messages.
/// </summary>
public static class MessageBusSchedulingExtensions
{
    /// <summary>
    /// Sends a message scheduled for delivery at the specified absolute time.
    /// </summary>
    /// <param name="bus">The message bus to send through.</param>
    /// <param name="message">The message instance to send.</param>
    /// <param name="scheduledTime">The absolute time at which the message should be delivered.</param>
    /// <param name="cancellationToken">A token to cancel the send operation.</param>
    /// <returns>A task that completes when the message has been handed off to the transport.</returns>
    public static ValueTask ScheduleSendAsync(
        this IMessageBus bus,
        object message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken = default)
        => bus.SendAsync(message, new SendOptions { ScheduledTime = scheduledTime }, cancellationToken);

    /// <summary>
    /// Sends a message scheduled for delivery after the specified delay.
    /// </summary>
    /// <param name="bus">The message bus to send through.</param>
    /// <param name="message">The message instance to send.</param>
    /// <param name="delay">The delay after which the message should be delivered.</param>
    /// <param name="timeProvider">The time provider used to compute the absolute scheduled time.</param>
    /// <param name="cancellationToken">A token to cancel the send operation.</param>
    /// <returns>A task that completes when the message has been handed off to the transport.</returns>
    public static ValueTask ScheduleSendAsync(
        this IMessageBus bus,
        object message,
        TimeSpan delay,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
        => bus.SendAsync(message, new SendOptions { ScheduledTime = timeProvider.GetUtcNow().Add(delay) }, cancellationToken);

    /// <summary>
    /// Publishes a message scheduled for delivery at the specified absolute time.
    /// </summary>
    /// <typeparam name="T">The type of the message to publish.</typeparam>
    /// <param name="bus">The message bus to publish through.</param>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="scheduledTime">The absolute time at which the message should be delivered.</param>
    /// <param name="cancellationToken">A token to cancel the publish operation.</param>
    /// <returns>A task that completes when the message has been handed off to the transport.</returns>
    public static ValueTask SchedulePublishAsync<T>(
        this IMessageBus bus,
        T message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken = default)
        where T : notnull
        => bus.PublishAsync(message, new PublishOptions { ScheduledTime = scheduledTime }, cancellationToken);

    /// <summary>
    /// Publishes a message scheduled for delivery after the specified delay.
    /// </summary>
    /// <typeparam name="T">The type of the message to publish.</typeparam>
    /// <param name="bus">The message bus to publish through.</param>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="delay">The delay after which the message should be delivered.</param>
    /// <param name="timeProvider">The time provider used to compute the absolute scheduled time.</param>
    /// <param name="cancellationToken">A token to cancel the publish operation.</param>
    /// <returns>A task that completes when the message has been handed off to the transport.</returns>
    public static ValueTask SchedulePublishAsync<T>(
        this IMessageBus bus,
        T message,
        TimeSpan delay,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
        where T : notnull
        => bus.PublishAsync(message, new PublishOptions { ScheduledTime = timeProvider.GetUtcNow().Add(delay) }, cancellationToken);
}
