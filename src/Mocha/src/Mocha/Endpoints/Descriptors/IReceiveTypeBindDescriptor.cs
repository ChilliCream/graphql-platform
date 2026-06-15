namespace Mocha;

/// <summary>
/// Fluent descriptor for per-type receive binding configuration on a receive endpoint.
/// Controls whether convention auto-binding is generated for the type.
/// </summary>
public interface IReceiveTypeBindDescriptor
{
    /// <summary>
    /// Overrides the auto-binding decision for this message type.
    /// When <c>true</c>, convention binds are generated for this type even if the queue or transport
    /// scope disables auto-binding. When <c>false</c>, no convention binds are generated for this
    /// type even if the queue or transport scope enables them.
    /// </summary>
    /// <param name="enabled">True to enable auto-binding for this type, false to disable it.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IReceiveTypeBindDescriptor AutoBind(bool enabled);
}
