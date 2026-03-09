namespace Mocha;

/// <summary>
/// Base class for receive endpoint descriptors that provides fluent configuration of consumer bindings, error handling, concurrency, and receive middleware.
/// </summary>
/// <typeparam name="T">The receive endpoint configuration type.</typeparam>
public abstract class ReceiveEndpointDescriptor<T>(IMessagingConfigurationContext context)
    : MessagingDescriptorBase<T>(context)
    , IReceiveEndpointDescriptor<T> where T : ReceiveEndpointConfiguration
{
    protected internal override T Configuration { get; protected set; } = null!;

    public IReceiveEndpointDescriptor<T> Handler<THandler>() where THandler : class, IHandler
    {
        Configuration.ConsumerIdentities.Add(typeof(THandler));
        return this;
    }

    public IReceiveEndpointDescriptor<T> Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        Configuration.ConsumerIdentities.Add(typeof(TConsumer));
        return this;
    }

    public IReceiveEndpointDescriptor<T> Kind(ReceiveEndpointKind kind)
    {
        Configuration.Kind = kind;
        return this;
    }

    public IReceiveEndpointDescriptor<T> MaxConcurrency(int maxConcurrency)
    {
        Configuration.MaxConcurrency = maxConcurrency;
        return this;
    }

    public IReceiveEndpointDescriptor<T> FaultEndpoint(string address)
    {
        Configuration.ErrorEndpoint = new Uri(address);
        return this;
    }

    public IReceiveEndpointDescriptor<T> SkippedEndpoint(string address)
    {
        Configuration.SkippedEndpoint = new Uri(address);
        return this;
    }

    public IReceiveEndpointDescriptor<T> UseReceive(ReceiveMiddlewareConfiguration configuration)
    {
        Configuration.ReceiveMiddlewares.Add(configuration);
        return this;
    }

    public IReceiveEndpointDescriptor<T> AppendReceive(string after, ReceiveMiddlewareConfiguration configuration)
    {
        Configuration.ReceivePipelineModifiers.Append(configuration, after);
        return this;
    }

    public IReceiveEndpointDescriptor<T> PrependReceive(string before, ReceiveMiddlewareConfiguration configuration)
    {
        Configuration.ReceivePipelineModifiers.Prepend(configuration, before);

        return this;
    }
}
