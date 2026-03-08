namespace Mocha.Transport.InMemory;

internal sealed class InMemoryDispatchEndpointDescriptor
    : DispatchEndpointDescriptor<InMemoryDispatchEndpointConfiguration>
    , IInMemoryDispatchEndpointDescriptor
{
    private InMemoryDispatchEndpointDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new InMemoryDispatchEndpointConfiguration { Name = name, TopicName = name };
    }

    protected override InMemoryDispatchEndpointConfiguration Configuration { get; set; }

    public IInMemoryDispatchEndpointDescriptor ToQueue(string name)
    {
        Configuration.QueueName = name;
        Configuration.TopicName = null;
        return this;
    }

    public IInMemoryDispatchEndpointDescriptor ToTopic(string name)
    {
        Configuration.QueueName = null;
        Configuration.TopicName = name;
        return this;
    }

    public new IInMemoryDispatchEndpointDescriptor Send<TMessage>()
    {
        base.Send<TMessage>();
        return this;
    }

    public new IInMemoryDispatchEndpointDescriptor Publish<TMessage>()
    {
        base.Publish<TMessage>();
        return this;
    }

    public new IInMemoryDispatchEndpointDescriptor UseDispatch(DispatchMiddlewareConfiguration configuration)
    {
        base.UseDispatch(configuration);
        return this;
    }

    public new IInMemoryDispatchEndpointDescriptor AppendDispatch(
        string after,
        DispatchMiddlewareConfiguration configuration)
    {
        base.AppendDispatch(after, configuration);
        return this;
    }

    public new IInMemoryDispatchEndpointDescriptor PrependDispatch(
        string before,
        DispatchMiddlewareConfiguration configuration)
    {
        base.PrependDispatch(before, configuration);
        return this;
    }

    public InMemoryDispatchEndpointConfiguration CreateConfiguration() => Configuration;

    public static InMemoryDispatchEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
