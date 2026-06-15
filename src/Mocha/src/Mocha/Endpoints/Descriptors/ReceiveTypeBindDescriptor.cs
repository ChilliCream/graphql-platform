namespace Mocha;

/// <summary>
/// Default implementation of <see cref="IReceiveTypeBindDescriptor"/> that collects per-type
/// auto-binding intents.
/// </summary>
public sealed class ReceiveTypeBindDescriptor : IReceiveTypeBindDescriptor
{
    private bool? _explicitAutoBind;

    /// <summary>
    /// Gets the resolved auto-binding decision for this type.
    /// Returns the explicit value if <see cref="AutoBind(bool)"/> was called, or <c>null</c>
    /// when it was not configured (inherit from queue or transport scope).
    /// </summary>
    public bool? ResolvedAutoBind => _explicitAutoBind;

    /// <inheritdoc />
    public IReceiveTypeBindDescriptor AutoBind(bool enabled)
    {
        _explicitAutoBind = enabled;
        return this;
    }
}
