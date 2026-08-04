namespace Mocha;

/// <summary>
/// Defines the naming conventions used to derive endpoint names, message identities, and saga names
/// from types and routes.
/// </summary>
public interface IBusNamingConventions
{
    /// <summary>
    /// Derives the receive endpoint name for the specified inbound route and endpoint kind.
    /// </summary>
    /// <param name="route">The inbound route to derive the name from.</param>
    /// <param name="kind">The kind of receive endpoint (default, error, skipped, or reply).</param>
    /// <returns>The derived endpoint name.</returns>
    string GetReceiveEndpointName(InboundRoute route, ReceiveEndpointKind kind);

    /// <summary>
    /// Derives the receive endpoint name for the specified handler type and endpoint kind.
    /// </summary>
    /// <param name="handlerType">The message handler type.</param>
    /// <param name="kind">The kind of receive endpoint.</param>
    /// <returns>The derived endpoint name.</returns>
    string GetReceiveEndpointName(Type handlerType, ReceiveEndpointKind kind);

    /// <summary>
    /// Derives the receive endpoint name from an explicit name and endpoint kind.
    /// </summary>
    /// <param name="name">The explicit endpoint name.</param>
    /// <param name="kind">The kind of receive endpoint.</param>
    /// <returns>The derived endpoint name with the appropriate suffix.</returns>
    string GetReceiveEndpointName(string name, ReceiveEndpointKind kind);

    /// <summary>
    /// Derives the saga name from the specified saga type.
    /// </summary>
    /// <param name="sagaType">The saga type.</param>
    /// <returns>The derived saga name.</returns>
    string GetSagaName(Type sagaType);

    /// <summary>
    /// Gets a unique instance-specific endpoint name for request-reply patterns.
    /// </summary>
    /// <param name="instanceId">The unique instance identifier.</param>
    /// <returns>A unique endpoint name for receiving replies.</returns>
    string GetInstanceEndpoint(Guid instanceId);

    /// <summary>
    /// Derives the send (point-to-point) endpoint name for the specified message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <returns>The derived send endpoint name.</returns>
    string GetSendEndpointName(Type messageType);

    /// <summary>
    /// Derives the publish (fan-out) endpoint name for the specified message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <returns>The derived publish endpoint name.</returns>
    string GetPublishEndpointName(Type messageType);

    /// <summary>
    /// Derives a URN-style message identity string for the specified message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <returns>A URN-formatted message identity string.</returns>
    string GetMessageIdentity(Type messageType);
}
