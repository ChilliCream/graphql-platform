namespace Mocha;

/// <summary>
/// Descriptor implementation for configuring an outbound message route.
/// </summary>
public class OutboundRouteDescriptor : MessagingDescriptorBase<OutboundRouteConfiguration>, IOutboundRouteDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundRouteDescriptor"/> class for the specified route kind.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="kind">The outbound route kind (send or publish).</param>
    public OutboundRouteDescriptor(IMessagingConfigurationContext context, OutboundRouteKind kind) : base(context)
    {
        Configuration = new OutboundRouteConfiguration { Kind = kind };
    }

    protected internal override OutboundRouteConfiguration Configuration { get; protected set; }

    /// <summary>
    /// Creates the final configuration from the descriptor state.
    /// </summary>
    /// <returns>The configured <see cref="OutboundRouteConfiguration"/>.</returns>
    public OutboundRouteConfiguration CreateConfiguration() => Configuration;

    /// <inheritdoc />
    public IOutboundRouteDescriptor Destination(Uri destination)
    {
        Configuration.Destination = destination;
        return this;
    }
}
