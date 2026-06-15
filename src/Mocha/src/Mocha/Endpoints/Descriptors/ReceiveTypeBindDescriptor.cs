namespace Mocha;

/// <summary>
/// Default implementation of <see cref="IReceiveTypeBindDescriptor"/> that collects per-type
/// bind intents.
/// </summary>
public sealed class ReceiveTypeBindDescriptor : IReceiveTypeBindDescriptor
{
    private MessagingBindMode? _explicitBindMode;

    /// <summary>
    /// Gets the resolved bind mode for this type.
    /// Returns the explicit value if <see cref="BindImplicitly"/> or <see cref="BindExplicitly"/>
    /// was called, or <c>null</c> when it was not configured (inherit from queue or transport scope).
    /// </summary>
    public MessagingBindMode? ResolvedBindMode => _explicitBindMode;

    /// <inheritdoc />
    public IReceiveTypeBindDescriptor BindImplicitly()
    {
        _explicitBindMode = MessagingBindMode.Implicit;
        return this;
    }

    /// <inheritdoc />
    public IReceiveTypeBindDescriptor BindExplicitly()
    {
        _explicitBindMode = MessagingBindMode.Explicit;
        return this;
    }
}
