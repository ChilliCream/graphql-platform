namespace Mocha.Transport.InMemory;

internal sealed class InMemoryReceiveEndpointDescriptor
    : ReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>
    , IInMemoryReceiveEndpointDescriptor
{
    internal InMemoryReceiveEndpointDescriptor(IMessagingConfigurationContext discoveryContext, string name)
        : base(discoveryContext)
    {
        Configuration = new InMemoryReceiveEndpointConfiguration { Name = name, QueueName = name };
    }

    public new IInMemoryReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        base.Handler<THandler>();

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        base.Consumer<TConsumer>();

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind)
    {
        base.Kind(kind);

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency)
    {
        base.MaxConcurrency(maxConcurrency);

        return this;
    }

    public IInMemoryReceiveEndpointDescriptor Queue(string name)
    {
        Configuration.QueueName = name;

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor FaultEndpoint(string name)
    {
        base.FaultEndpoint(name);

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor SkippedEndpoint(string name)
    {
        base.SkippedEndpoint(name);

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor UseReceive(ReceiveMiddlewareConfiguration configuration)
    {
        base.UseReceive(configuration);

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor AppendReceive(
        string after,
        ReceiveMiddlewareConfiguration configuration)
    {
        base.AppendReceive(after, configuration);

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor PrependReceive(
        string before,
        ReceiveMiddlewareConfiguration configuration)
    {
        base.PrependReceive(before, configuration);

        return this;
    }

    public InMemoryReceiveEndpointConfiguration CreateConfiguration()
    {
        return Configuration;
    }

    public static InMemoryReceiveEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
