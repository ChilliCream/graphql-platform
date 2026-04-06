using System.ComponentModel;

namespace Mocha;

/// <summary>
/// Pre-built message type configuration emitted by the source generator.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MessagingMessageConfiguration
{
    /// <summary>
    /// The CLR type of the message.
    /// </summary>
    public required Type MessageType { get; init; }

    /// <summary>
    /// The pre-built serializer for this message type.
    /// </summary>
    public required IMessageSerializer Serializer { get; init; }

    /// <summary>
    /// Pre-computed type hierarchy sorted by specificity (most specific first).
    /// Contains only registered user types — framework base types (<see cref="IEventRequest"/>,
    /// <see cref="IEventRequest{T}"/>) are excluded. When non-null, <see cref="MessageType.Complete"/>
    /// skips reflection-based hierarchy discovery.
    /// </summary>
    public Type[]? EnclosedTypes { get; init; }
}
