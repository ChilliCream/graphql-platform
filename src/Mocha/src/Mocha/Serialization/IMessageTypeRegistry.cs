namespace Mocha;

/// <summary>
/// Maintains the registry of all known message types and provides lookup by CLR type or identity string.
/// </summary>
public interface IMessageTypeRegistry
{
    /// <summary>
    /// Gets the serializer registry used to resolve serializers for registered message types.
    /// </summary>
    IMessageSerializerRegistry Serializers { get; }

    /// <summary>
    /// Gets the set of all registered message types.
    /// </summary>
    IReadOnlySet<MessageType> MessageTypes { get; }

    /// <summary>
    /// Determines whether the specified CLR type is registered as a message type.
    /// </summary>
    /// <param name="type">The CLR type to check.</param>
    /// <returns><c>true</c> if the type is registered; otherwise, <c>false</c>.</returns>
    bool IsRegistered(Type type);

    /// <summary>
    /// Gets the message type metadata for the specified CLR type, or <c>null</c> if not registered.
    /// </summary>
    /// <param name="type">The CLR type to look up.</param>
    /// <returns>The message type metadata, or <c>null</c>.</returns>
    MessageType? GetMessageType(Type type);

    /// <summary>
    /// Gets the message type metadata for the specified identity string, or <c>null</c> if not registered.
    /// </summary>
    /// <param name="identity">The message type identity (URN).</param>
    /// <returns>The message type metadata, or <c>null</c>.</returns>
    MessageType? GetMessageType(string identity);

    /// <summary>
    /// Registers a message type in the registry.
    /// </summary>
    /// <param name="messageType">The message type to register.</param>
    void AddMessageType(MessageType messageType);

    /// <summary>
    /// Gets the message type metadata for the specified CLR type, creating and registering it if not already present.
    /// </summary>
    /// <param name="context">The messaging configuration context used for initialization.</param>
    /// <param name="type">The CLR type to look up or register.</param>
    /// <returns>The existing or newly created message type metadata.</returns>
    MessageType GetOrAdd(IMessagingConfigurationContext context, Type type);
}
