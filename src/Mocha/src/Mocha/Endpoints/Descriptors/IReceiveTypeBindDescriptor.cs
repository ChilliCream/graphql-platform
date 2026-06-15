namespace Mocha;

/// <summary>
/// Fluent descriptor for per-type receive binding configuration on a receive endpoint.
/// Controls whether convention binds are generated for the type.
/// </summary>
public interface IReceiveTypeBindDescriptor
{
    /// <summary>
    /// Sets the bind mode for this message type to <see cref="MessagingBindMode.Implicit"/>,
    /// generating convention binds for this type even if the queue or transport scope suppresses them.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IReceiveTypeBindDescriptor BindImplicitly();

    /// <summary>
    /// Sets the bind mode for this message type to <see cref="MessagingBindMode.Explicit"/>,
    /// suppressing convention binds for this type even if the queue or transport scope enables them.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IReceiveTypeBindDescriptor BindExplicitly();
}
