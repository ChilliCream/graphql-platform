namespace Mocha;

/// <summary>
/// Base class for dispatch endpoint descriptors that provides fluent configuration of routes and dispatch middleware.
/// </summary>
/// <typeparam name="T">The dispatch endpoint configuration type.</typeparam>
public abstract class DispatchEndpointDescriptor<T>(IMessagingConfigurationContext context)
    : MessagingDescriptorBase<T>(context)
    , IDispatchEndpointDescriptor<T> where T : DispatchEndpointConfiguration
{
    protected internal override T Configuration { get; protected set; } = null!;

    public IDispatchEndpointDescriptor<T> Send<TMessage>()
    {
        Configuration.Routes.Add((typeof(TMessage), OutboundRouteKind.Send));
        return this;
    }

    public IDispatchEndpointDescriptor<T> Publish<TMessage>()
    {
        Configuration.Routes.Add((typeof(TMessage), OutboundRouteKind.Publish));
        return this;
    }

    public IDispatchEndpointDescriptor<T> UseDispatch(DispatchMiddlewareConfiguration configuration)
    {
        Configuration.DispatchMiddlewares.Add(configuration);
        return this;
    }

    public IDispatchEndpointDescriptor<T> AppendDispatch(string after, DispatchMiddlewareConfiguration configuration)
    {
        Configuration.DispatchPipelineModifiers.Append(configuration, after);
        return this;
    }

    public IDispatchEndpointDescriptor<T> PrependDispatch(string before, DispatchMiddlewareConfiguration configuration)
    {
        Configuration.DispatchPipelineModifiers.Prepend(configuration, before);
        return this;
    }
}
