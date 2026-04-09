using Confluent.Kafka;
using Mocha.Features;

namespace Mocha.Transport.Kafka.Features;

/// <summary>
/// Pooled feature that carries the Kafka consume result and consumer reference through the receive middleware pipeline,
/// enabling acknowledgement and message parsing middleware to access the raw delivery context.
/// </summary>
public sealed class KafkaReceiveFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets the Kafka consume result containing the message body, headers, and offset metadata.
    /// </summary>
    public ConsumeResult<byte[], byte[]> ConsumeResult { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Kafka consumer instance that received the message, used for manual offset commits.
    /// </summary>
    public IConsumer<byte[], byte[]> Consumer { get; set; } = null!;

    /// <summary>
    /// Gets the topic from which the message was consumed.
    /// </summary>
    public string Topic => ConsumeResult.Topic;

    /// <summary>
    /// Gets the partition from which the message was consumed.
    /// </summary>
    public int Partition => ConsumeResult.Partition.Value;

    /// <summary>
    /// Gets the offset of the consumed message within the partition.
    /// </summary>
    public long Offset => ConsumeResult.Offset.Value;

    /// <inheritdoc />
    public void Initialize(object state)
    {
        ConsumeResult = null!;
        Consumer = null!;
    }

    /// <inheritdoc />
    public void Reset()
    {
        ConsumeResult = null!;
        Consumer = null!;
    }
}
