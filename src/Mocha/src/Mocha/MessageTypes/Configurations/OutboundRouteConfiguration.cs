namespace Mocha;

/// <summary>
/// Configuration for an outbound message route, specifying the route kind, message type, and destination.
/// </summary>
public class OutboundRouteConfiguration : MessagingConfiguration
{
    /// <summary>
    /// Gets or sets the kind of outbound route (send or publish).
    /// </summary>
    public OutboundRouteKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the resolved message type, or <c>null</c> if using <see cref="RuntimeType"/> instead.
    /// </summary>
    public MessageType? MessageType { get; set; }

    /// <summary>
    /// Gets or sets the CLR type of the message, used when <see cref="MessageType"/> is not set.
    /// </summary>
    public Type? RuntimeType { get; set; }

    /// <summary>
    /// Gets or sets the destination URI for this route, or <c>null</c> to use naming conventions.
    /// </summary>
    public Uri? Destination { get; set; }
}
