using System.Collections.Immutable;

namespace Mocha;

/// <summary>
/// Configuration for a message type, specifying its identity, CLR type, default content type, serializers, and outbound routes.
/// </summary>
public class MessageTypeConfiguration : MessagingConfiguration
{
    /// <summary>
    /// Gets or sets the URN-based identity string for this message type.
    /// </summary>
    public string? Identity { get; set; }

    /// <summary>
    /// Gets or sets the CLR type that this message type represents.
    /// </summary>
    public Type? RuntimeType { get; set; }

    /// <summary>
    /// Gets or sets the default content type for serialization.
    /// </summary>
    public MessageContentType? DefaultContentType { get; set; }

    /// <summary>
    /// Gets or sets the content-type-specific serializers for this message type.
    /// </summary>
    public Dictionary<MessageContentType, IMessageSerializer> MessageSerializer { get; set; } = [];

    /// <summary>
    /// Gets or sets the outbound route configurations for this message type.
    /// </summary>
    public List<OutboundRouteConfiguration> Routes { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether this message type is internal (not exposed for external routing).
    /// </summary>
    public bool IsInternal { get; set; }

    /// <summary>
    /// Pre-computed enclosed types from source generator. Contains both concrete registered
    /// types and framework base types (for example, <see cref="IEventRequest"/> and
    /// <see cref="IEventRequest{T}"/>). <see cref="ImmutableArray{T}.IsDefaultOrEmpty"/>
    /// indicates the source generator did not supply this information.
    /// </summary>
    public ImmutableArray<Type> EnclosedTypes { get; set; }
}
