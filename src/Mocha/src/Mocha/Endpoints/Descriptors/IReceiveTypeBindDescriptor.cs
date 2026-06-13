namespace Mocha;

/// <summary>
/// Fluent descriptor for per-type receive binding configuration on a receive endpoint.
/// Controls whether convention auto-binding is generated for the type and records explicit
/// binding intents from source entities to this endpoint's queue.
/// </summary>
public interface IReceiveTypeBindDescriptor
{
    /// <summary>
    /// Overrides the auto-binding decision for this message type.
    /// When <c>true</c>, convention binds are generated for this type even if the queue or transport
    /// scope disables auto-binding. When <c>false</c>, no convention binds are generated for this
    /// type even if the queue or transport scope enables them. An explicit call always wins over the
    /// implication from <see cref="BindFrom(Uri, string?)"/>, regardless of call order.
    /// </summary>
    /// <param name="enabled">True to enable auto-binding for this type, false to disable it.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IReceiveTypeBindDescriptor AutoBind(bool enabled);

    /// <summary>
    /// Declares an explicit binding from the specified source entity into this endpoint's queue
    /// for this message type. Calling this method implies <c>AutoBind(false)</c> for this type
    /// unless <see cref="AutoBind(bool)"/> is called explicitly with <c>true</c>.
    /// </summary>
    /// <param name="source">The URI of the source exchange, queue, or topic to bind from.</param>
    /// <param name="routingKey">
    /// The optional routing key for the binding. When <c>null</c>, the binding matches all
    /// messages from the source.
    /// </param>
    /// <returns>The descriptor for method chaining.</returns>
    IReceiveTypeBindDescriptor BindFrom(Uri source, string? routingKey = null);
}
