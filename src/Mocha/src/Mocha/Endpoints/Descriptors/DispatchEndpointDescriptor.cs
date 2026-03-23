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

    public IDispatchEndpointDescriptor<T> UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        if (before is not null && after is not null)
        {
            throw new ArgumentException(
                "Only one of 'before' or 'after' can be specified at the same time.");
        }

        if (before is null && after is null)
        {
            Configuration.DispatchMiddlewares.Add(configuration);
            return this;
        }

        if (before is not null)
        {
            Configuration.DispatchPipelineModifiers.Prepend(configuration, before);
        }
        else
        {
            Configuration.DispatchPipelineModifiers.Append(configuration, after);
        }

        return this;
    }
}
