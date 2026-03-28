using Mocha.Features;
using NATS.Client.JetStream;

namespace Mocha.Transport.NATS.Features;

/// <summary>
/// Pooled feature that carries the raw <see cref="INatsJSMsg{T}"/> through the receive middleware pipeline,
/// enabling acknowledgement and message parsing middleware to access the JetStream delivery context.
/// </summary>
public sealed class NatsReceiveFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets the JetStream message that was delivered to the receive endpoint.
    /// </summary>
    public INatsJSMsg<ReadOnlyMemory<byte>>? Message { get; set; }

    /// <inheritdoc />
    public void Initialize(object state)
    {
        Message = null;
    }

    /// <inheritdoc />
    public void Reset()
    {
        Message = null;
    }
}
