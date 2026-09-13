using Mocha.Features;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Mocha.Transport.Nats.Features;

/// <summary>
/// Carries the message being processed through the receive pipeline, so that the parsing and
/// acknowledgement middlewares can reach it.
/// </summary>
// The payload is exposed separately from Message because reply endpoints receive over core NATS,
// where there is no JetStream message and nothing to acknowledge.
public sealed class NatsReceiveFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets the JetStream message being processed, or <see langword="null"/> when the
    /// message arrived over a core NATS subscription.
    /// </summary>
    public INatsJSMsg<ReadOnlyMemory<byte>>? Message { get; set; }

    /// <summary>
    /// Gets or sets the headers of the received message.
    /// </summary>
    public NatsHeaders? Headers { get; set; }

    /// <summary>
    /// Gets or sets the raw payload of the received message.
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; set; }

    /// <summary>
    /// Gets or sets how many times delivery has been attempted.
    /// </summary>
    public int DeliveryCount { get; set; }

    /// <summary>
    /// Gets or sets how often the message reports progress to extend its acknowledgement deadline,
    /// or <see langword="null"/> when progress is never reported.
    /// </summary>
    public TimeSpan? AckProgressInterval { get; set; }

    /// <inheritdoc />
    public void Initialize(object state) => Reset();

    /// <inheritdoc />
    public void Reset()
    {
        Message = null;
        Headers = null;
        Body = default;
        DeliveryCount = 0;
        AckProgressInterval = null;
    }
}
