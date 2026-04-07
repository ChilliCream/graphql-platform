namespace Mocha;

/// <summary>
/// Descriptor implementation for configuring an inbound message route.
/// </summary>
public class InboundRouteDescriptor : MessagingDescriptorBase<InboundRouteConfiguration>, IInboundRouteDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InboundRouteDescriptor"/> class for the specified route kind.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="kind">The inbound route kind.</param>
    public InboundRouteDescriptor(IMessagingConfigurationContext context, InboundRouteKind kind) : base(context)
    {
        Configuration = new InboundRouteConfiguration { Kind = kind };
    }

    protected internal override InboundRouteConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IInboundRouteDescriptor MessageType(Type messageType)
    {
        Configuration.MessageRuntimeType = messageType;
        return this;
    }

    /// <inheritdoc />
    public IInboundRouteDescriptor ResponseType(Type responseType)
    {
        Configuration.ResponseRuntimeType = responseType;
        return this;
    }

    /// <inheritdoc />
    public IInboundRouteDescriptor Kind(InboundRouteKind kind)
    {
        Configuration.Kind = kind;
        return this;
    }

    /// <summary>
    /// Creates the final configuration from the descriptor state.
    /// </summary>
    /// <returns>The configured <see cref="InboundRouteConfiguration"/>.</returns>
    public InboundRouteConfiguration CreateConfiguration() => Configuration;
}
