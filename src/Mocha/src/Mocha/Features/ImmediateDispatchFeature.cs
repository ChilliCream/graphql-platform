using Mocha.Features;

namespace Mocha;

/// <summary>
/// A pooled feature that marks a dispatch as leaving for the transport at once. The message is not
/// held until the consumer attempt completes and not stored by a middleware that defers delivery,
/// such as the outbox.
/// </summary>
public sealed class ImmediateDispatchFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets whether the current dispatch leaves at once.
    /// </summary>
    public bool Enabled { get; set; }

    /// <inheritdoc />
    public void Initialize(object state)
    {
        Enabled = false;
    }

    /// <inheritdoc />
    public void Reset()
    {
        Enabled = false;
    }
}
