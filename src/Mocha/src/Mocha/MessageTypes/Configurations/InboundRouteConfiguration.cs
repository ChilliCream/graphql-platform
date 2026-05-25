namespace Mocha;

/// <summary>
/// Configuration for an inbound message route, specifying the message type, consumer, and route kind.
/// </summary>
public class InboundRouteConfiguration : MessagingConfiguration
{
    /// <summary>
    /// Gets or sets the CLR type of the message, used when <see cref="MessageType"/> is not set.
    /// </summary>
    public Type? MessageRuntimeType { get; set; }

    /// <summary>
    /// Gets or sets the resolved message type, or <c>null</c> if using <see cref="MessageRuntimeType"/> instead.
    /// </summary>
    public MessageType? MessageType { get; set; }

    /// <summary>
    /// Gets or sets the CLR type of the response message for request-reply patterns.
    /// </summary>
    public Type? ResponseRuntimeType { get; set; }

    /// <summary>
    /// Gets or sets the consumer that handles messages arriving on this route.
    /// </summary>
    public Consumer? Consumer { get; set; }

    /// <summary>
    /// Gets or sets the kind of inbound route.
    /// </summary>
    public InboundRouteKind Kind { get; set; }
}
