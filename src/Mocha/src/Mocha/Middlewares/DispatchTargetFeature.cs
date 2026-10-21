using Mocha.Features;

namespace Mocha.Middlewares;

/// <summary>
/// Identifies an explicit target for the current outgoing message.
/// </summary>
internal sealed class DispatchTargetFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets the target, or null for ordinary dispatch.
    /// </summary>
    public IMessageDispatchTarget? Target { get; set; }

    /// <inheritdoc />
    public void Initialize(object state) => Target = null;

    /// <inheritdoc />
    public void Reset() => Target = null;
}
