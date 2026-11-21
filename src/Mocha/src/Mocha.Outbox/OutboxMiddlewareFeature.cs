using Mocha.Features;

namespace Mocha.Outbox;

/// <summary>
/// Controls whether the outbox middleware persists the current dispatch.
/// </summary>
public sealed class OutboxMiddlewareFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets a value indicating whether the message is sent to the transport instead of the outbox.
    /// The value has no effect for a message published or sent through a DbContext outbox.
    /// </summary>
    public bool SkipOutbox { get; set; }

    /// <inheritdoc />
    public void Initialize(object state)
    {
        SkipOutbox = false;
    }

    /// <inheritdoc />
    public void Reset()
    {
        SkipOutbox = false;
    }
}
