using Mocha.Features;

namespace Mocha;

/// <summary>
/// A pooled feature that carries the scheduling token assigned by the scheduled message store
/// back to the message bus after pipeline execution.
/// </summary>
public sealed class ScheduledMessageFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets the opaque scheduling token returned by the store after persistence.
    /// </summary>
    public string? Token { get; set; }

    /// <inheritdoc />
    public void Initialize(object state)
    {
        Token = null;
    }

    /// <inheritdoc />
    public void Reset()
    {
        Token = null;
    }
}
