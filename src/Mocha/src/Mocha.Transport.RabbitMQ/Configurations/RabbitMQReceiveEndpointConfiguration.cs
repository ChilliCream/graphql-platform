namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Configuration for a RabbitMQ receive endpoint, specifying the source queue and consumer prefetch settings.
/// </summary>
public sealed class RabbitMQReceiveEndpointConfiguration : ReceiveEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the RabbitMQ queue name from which this endpoint consumes messages.
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of unacknowledged messages the broker will deliver to this endpoint's consumer.
    /// Defaults to 100.
    /// </summary>
    public ushort MaxPrefetch { get; set; } = 100;

    /// <summary>
    /// Gets or sets the queue expiry applied to a temporary endpoint's queue. When <see langword="null"/>
    /// and <see cref="ReceiveEndpointConfiguration.IsTemporary"/> is set, <see cref="TemporaryDefaults.Expiry"/> applies.
    /// </summary>
    public TimeSpan? TemporaryExpiry { get; set; }

    public static class TemporaryDefaults
    {
        /// <summary>
        /// The queue expiry applied to a temporary endpoint marked via the parameterless
        /// <c>Temporary()</c> overload.
        /// </summary>
        public static readonly TimeSpan Expiry = TimeSpan.FromMinutes(30);

        /// <summary>
        /// The longest queue expiry the transport can declare through <c>x-expires</c>.
        /// </summary>
        public static readonly TimeSpan MaximumExpiry = TimeSpan.FromMilliseconds(int.MaxValue);
    }
}
