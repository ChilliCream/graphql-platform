namespace Mocha;

/// <summary>
/// Descriptor implementation for configuring a message type with serializers and outbound routes.
/// </summary>
public class MessageTypeDescriptor : MessagingDescriptorBase<MessageTypeConfiguration>, IMessageTypeDescriptor
{
    private readonly List<OutboundRouteDescriptor> _routes = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageTypeDescriptor"/> class for the specified message type.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="messageType">The CLR type of the message being configured.</param>
    public MessageTypeDescriptor(IMessagingConfigurationContext context, Type messageType) : base(context)
    {
        Configuration = new MessageTypeConfiguration { RuntimeType = messageType };
    }

    protected internal override MessageTypeConfiguration Configuration { get; protected set; }

    /// <summary>
    /// Creates the final configuration from the descriptor state.
    /// </summary>
    /// <returns>The configured <see cref="MessageTypeConfiguration"/>.</returns>
    public MessageTypeConfiguration CreateConfiguration()
    {
        Configuration.Routes = _routes.ConvertAll(r => r.CreateConfiguration());
        return Configuration;
    }

    /// <inheritdoc />
    public IMessageTypeDescriptor AddSerializer(IMessageSerializer messageSerializer)
    {
        Configuration.MessageSerializer[messageSerializer.ContentType] = messageSerializer;
        return this;
    }

    /// <summary>
    /// Sets the default content type for serialization of this message type.
    /// </summary>
    /// <param name="contentType">The default content type.</param>
    /// <returns>This descriptor for method chaining.</returns>
    public IMessageTypeDescriptor DefaultContentType(MessageContentType contentType)
    {
        Configuration.DefaultContentType = contentType;
        return this;
    }

    /// <inheritdoc />
    public IMessageTypeDescriptor Publish(Action<IOutboundRouteDescriptor> configure)
    {
        var descriptor = _routes.FirstOrDefault(r => r.Configuration.Kind == OutboundRouteKind.Publish);

        if (descriptor is null)
        {
            descriptor = new OutboundRouteDescriptor(Context, OutboundRouteKind.Publish);
            _routes.Add(descriptor);
        }

        configure(descriptor);

        return this;
    }

    /// <inheritdoc />
    public IMessageTypeDescriptor Send(Action<IOutboundRouteDescriptor> configure)
    {
        var descriptor = _routes.FirstOrDefault(r => r.Configuration.Kind == OutboundRouteKind.Send);

        if (descriptor is null)
        {
            descriptor = new OutboundRouteDescriptor(Context, OutboundRouteKind.Send);
            _routes.Add(descriptor);
        }

        configure(descriptor);

        return this;
    }
}
