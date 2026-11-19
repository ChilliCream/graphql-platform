namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// The timeouts and the retry delay of the consumers of a <see cref="RabbitMQConsumerManager"/>.
/// </summary>
internal sealed class RabbitMQConsumerManagerOptions
{
    /// <summary>
    /// Gets how long a consumer can take to open a channel, and how long it can then take to declare its queue
    /// and start consuming. An attempt that exceeds one of the two limits is abandoned and retried.
    /// </summary>
    public TimeSpan AttachTimeout { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets how long a consumer waits for a handler of a lost channel to return before it reports that it is
    /// blocked.
    /// </summary>
    public TimeSpan BlockedGracePeriod { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the delay before the second attempt to attach a consumer. The delay doubles with each attempt.
    /// </summary>
    public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets the largest delay between two attempts to attach a consumer.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets how long the deletion of a queue waits for the broker.
    /// </summary>
    public TimeSpan QueueDeleteTimeout { get; init; } = TimeSpan.FromSeconds(5);
}
