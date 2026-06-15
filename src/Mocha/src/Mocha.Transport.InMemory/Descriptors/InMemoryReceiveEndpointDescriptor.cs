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

    public new IInMemoryReceiveEndpointDescriptor Handler(Type handlerType)
    {
        base.Handler(handlerType);

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor Consumer(Type consumerType)
    {
        base.Consumer(consumerType);

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        base.Consumer<TConsumer>();

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor Receives<TMessage>()
    {
        base.Receives<TMessage>();

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor Receives(Type messageType)
    {
        base.Receives(messageType);

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor BindImplicitly()
    {
        base.BindImplicitly();

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor BindExplicitly()
    {
        base.BindExplicitly();

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

    public new IInMemoryReceiveEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseReceive(configuration, before: before, after: after);

        return this;
    }

    public InMemoryReceiveEndpointConfiguration CreateConfiguration()
    {
        return Configuration;
    }

    public static InMemoryReceiveEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
