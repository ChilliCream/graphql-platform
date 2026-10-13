using Mocha.Features;

namespace Mocha;

/// <summary>
/// A pooled feature that marks a dispatch whose message is held by a <see cref="DeferredDispatchScope"/>
/// until the consumer attempt that dispatched it completes.
/// </summary>
internal sealed class DeferredDispatchFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets the scope that holds the message, or <see langword="null"/> when the message leaves at once.
    /// </summary>
    public DeferredDispatchScope? Scope { get; set; }

    /// <summary>
    /// Gets or sets whether the dispatch pipeline handed the message to <see cref="Scope"/> instead of
    /// the transport.
    /// </summary>
    public bool IsHeld { get; set; }

    /// <inheritdoc />
    public void Initialize(object state)
    {
        Scope = null;
        IsHeld = false;
    }

    /// <inheritdoc />
    public void Reset()
    {
        Scope = null;
        IsHeld = false;
    }
}
