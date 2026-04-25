namespace Mocha;

/// <summary>
/// Describes a registered message type for diagnostic and visualization purposes.
/// </summary>
/// <param name="Identity">The canonical identity string for this message type.</param>
/// <param name="RuntimeType">The short CLR type name.</param>
/// <param name="RuntimeTypeFullName">The fully qualified CLR type name, or <c>null</c> if unavailable.</param>
/// <param name="IsInterface">Whether this message type is an interface-based message.</param>
/// <param name="IsInternal">Whether this message type is internal to the bus and not exposed to user code.</param>
/// <param name="DefaultContentType">The default serialization content type, or <c>null</c> if using the bus default.</param>
/// <param name="EnclosedMessageIdentities">The identities of message types enclosed by this type, or <c>null</c> if none.</param>
internal sealed record MessageTypeDescription(
    string Identity,
    string RuntimeType,
    string? RuntimeTypeFullName,
    bool IsInterface,
    bool IsInternal,
    string? DefaultContentType,
    IReadOnlyList<string>? EnclosedMessageIdentities);
