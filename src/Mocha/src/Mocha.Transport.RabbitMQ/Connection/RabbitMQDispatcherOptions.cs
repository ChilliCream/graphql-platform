namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// The limits of a <see cref="RabbitMQDispatcher"/>.
/// </summary>
internal sealed class RabbitMQDispatcherOptions
{
    /// <summary>
    /// Gets the largest number of publisher channels, which is also the largest number of publishes in flight.
    /// </summary>
    public int MaxPublisherChannels { get; init; } = 100;

    /// <summary>
    /// Gets how long the open of a publisher channel can take before its connection is closed and replaced.
    /// </summary>
    public TimeSpan ChannelOpenTimeout { get; init; } = TimeSpan.FromSeconds(60);
}
