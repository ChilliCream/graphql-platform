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

    /// <inheritdoc />
    public void Initialize(object state)
    {
        Scope = null;
    }

    /// <inheritdoc />
    public void Reset()
    {
        Scope = null;
    }
}
