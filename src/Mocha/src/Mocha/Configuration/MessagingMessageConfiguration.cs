using System.Collections.Immutable;
using System.ComponentModel;

namespace Mocha;

/// <summary>
/// Represents a pre-built message type configuration emitted by the source generator.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MessagingMessageConfiguration
{
    /// <summary>
    /// Gets the CLR type of the message.
    /// </summary>
    public required Type MessageType { get; init; }

    /// <summary>
    /// Gets the pre-built serializer for this message type.
    /// </summary>
    public required IMessageSerializer Serializer { get; init; }

    /// <summary>
    /// Gets the pre-computed type hierarchy sorted by specificity (most specific first). Includes both
    /// registered user types and framework base types (<see cref="IEventRequest"/>,
    /// <see cref="IEventRequest{T}"/>); <see cref="MessageType.Complete"/> branches on whether
    /// each entry is a framework base type to decide registration versus identity-only recording.
    /// When <see cref="ImmutableArray{T}.IsDefaultOrEmpty"/> is <see langword="true"/>,
    /// <see cref="MessageType.Complete"/> falls back to reflection-based hierarchy discovery
    /// (non-AOT only).
    /// </summary>
    public ImmutableArray<Type> EnclosedTypes { get; init; }
}
