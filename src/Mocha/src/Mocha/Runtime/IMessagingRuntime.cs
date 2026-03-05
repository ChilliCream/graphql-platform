namespace Mocha;

/// <summary>
/// Represents the fully initialized messaging runtime, providing access to endpoints, message types, transports, and configuration options.
/// </summary>
public interface IMessagingRuntime : IMessagingRuntimeContext
{
    /// <summary>
    /// Gets the read-only messaging options that were used to configure this runtime.
    /// </summary>
    IReadOnlyMessagingOptions Options { get; }

    /// <summary>
    /// Gets the dispatch endpoint configured for sending (point-to-point) the specified message type.
    /// </summary>
    /// <param name="messageType">The message type to look up.</param>
    /// <returns>The dispatch endpoint for sending this message type.</returns>
    DispatchEndpoint GetSendEndpoint(MessageType messageType);

    /// <summary>
    /// Gets the dispatch endpoint configured for publishing (fan-out) the specified message type.
    /// </summary>
    /// <param name="messageType">The message type to look up.</param>
    /// <returns>The dispatch endpoint for publishing this message type.</returns>
    DispatchEndpoint GetPublishEndpoint(MessageType messageType);

    /// <summary>
    /// Gets the dispatch endpoint for the specified destination address.
    /// </summary>
    /// <param name="address">The destination URI.</param>
    /// <returns>The dispatch endpoint for the given address.</returns>
    DispatchEndpoint GetDispatchEndpoint(Uri address);

    /// <summary>
    /// Gets the registered message type metadata for the specified CLR type.
    /// </summary>
    /// <param name="type">The CLR type of the message.</param>
    /// <returns>The message type metadata.</returns>
    MessageType GetMessageType(Type type);

    /// <summary>
    /// Gets the registered message type metadata for the specified identity string, or <c>null</c> if not found.
    /// </summary>
    /// <param name="identity">The message type identity string (URN).</param>
    /// <returns>The message type metadata, or <c>null</c> if no message type matches the identity.</returns>
    MessageType? GetMessageType(string? identity);

    /// <summary>
    /// Gets the transport associated with the specified address, or <c>null</c> if no transport handles that scheme.
    /// </summary>
    /// <param name="address">The address URI whose scheme identifies the transport.</param>
    /// <returns>The messaging transport, or <c>null</c> if no matching transport is found.</returns>
    MessagingTransport? GetTransport(Uri address);
}
