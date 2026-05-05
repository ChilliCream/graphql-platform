namespace Mocha.Transport.AzureServiceBus;

internal sealed class AzureServiceBusDispatchEndpointDescriptor
    : DispatchEndpointDescriptor<AzureServiceBusDispatchEndpointConfiguration>
    , IAzureServiceBusDispatchEndpointDescriptor
{
    private AzureServiceBusDispatchEndpointDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new AzureServiceBusDispatchEndpointConfiguration { Name = name, TopicName = name };
    }

    /// <inheritdoc />
    protected internal override AzureServiceBusDispatchEndpointConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IAzureServiceBusDispatchEndpointDescriptor ToQueue(string name)
    {
        Configuration.QueueName = name;
        Configuration.TopicName = null;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusDispatchEndpointDescriptor ToTopic(string name)
    {
        Configuration.QueueName = null;
        Configuration.TopicName = name;
        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusDispatchEndpointDescriptor Send<TMessage>()
    {
        base.Send<TMessage>();
        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusDispatchEndpointDescriptor Publish<TMessage>()
    {
        base.Publish<TMessage>();
        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusDispatchEndpointDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseDispatch(configuration, before: before, after: after);
        return this;
    }

    /// <inheritdoc />
    public AzureServiceBusDispatchEndpointConfiguration CreateConfiguration() => Configuration;

    public static AzureServiceBusDispatchEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
