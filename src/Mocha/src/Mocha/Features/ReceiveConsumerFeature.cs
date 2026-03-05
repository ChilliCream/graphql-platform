using Mocha.Features;

namespace Mocha;

/// <summary>
/// A pooled feature that tracks which consumers are eligible to handle an incoming message and the
/// current execution state of consumer dispatch.
/// </summary>
public sealed class ReceiveConsumerFeature : IPooledFeature
{
    /// <summary>
    /// Gets the set of consumers that are bound to handle the current message.
    /// </summary>
    public HashSet<Consumer> Consumers { get; } = [];

    /// <summary>
    /// Gets or sets the consumer that is currently executing within the consumer middleware
    /// pipeline.
    /// </summary>
    public Consumer? CurrentConsumer { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the message has been successfully consumed by at
    /// least one consumer.
    /// </summary>
    public bool MessageConsumed { get; set; }

    public void Initialize(object state)
    {
        Consumers.Clear();
        CurrentConsumer = null;
        MessageConsumed = false;
    }

    public void Reset()
    {
        Consumers.Clear();
        CurrentConsumer = null;
        MessageConsumed = false;
    }
}
