namespace Mocha;

/// <summary>
/// Provides a fluent API for configuring a consumer's name, inbound routes, and consumer-scoped
/// middleware during bus setup.
/// </summary>
public class ConsumerDescriptor : MessagingDescriptorBase<ConsumerConfiguration>, IConsumerDescriptor
{
    private readonly List<InboundRouteDescriptor> _routes = [];

    /// <summary>
    /// Creates a new consumer descriptor within the given messaging configuration context.
    /// </summary>
    /// <param name="context">
    /// The messaging configuration context providing access to naming, routing, and conventions.
    /// </param>
    public ConsumerDescriptor(IMessagingConfigurationContext context) : base(context)
    {
        Configuration = new ConsumerConfiguration();
    }

    protected internal override ConsumerConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IConsumerDescriptor Name(string name)
    {
        Configuration.Name = name;
        return this;
    }

    /// <inheritdoc />
    public IConsumerDescriptor AddRoute(Action<IInboundRouteDescriptor> configure)
    {
        var descriptor = new InboundRouteDescriptor(Context, InboundRouteKind.Subscribe);
        configure(descriptor);
        _routes.Add(descriptor);
        return this;
    }

    /// <inheritdoc />
    public IConsumerDescriptor UseConsumer(ConsumerMiddlewareConfiguration configuration)
    {
        Configuration.ConsumerMiddlewares.Add(configuration);
        return this;
    }

    /// <inheritdoc />
    public IConsumerDescriptor AppendConsumer(string after, ConsumerMiddlewareConfiguration configuration)
    {
        Configuration.ConsumerPipelineModifiers.Append(configuration, after);
        return this;
    }

    /// <inheritdoc />
    public IConsumerDescriptor PrependConsumer(string before, ConsumerMiddlewareConfiguration configuration)
    {
        Configuration.ConsumerPipelineModifiers.Prepend(configuration, before);
        return this;
    }

    /// <summary>
    /// Builds and returns the finalized <see cref="ConsumerConfiguration"/> from this descriptor's
    /// accumulated settings.
    /// </summary>
    /// <returns>The consumer configuration ready for runtime initialization.</returns>
    public ConsumerConfiguration CreateConfiguration()
    {
        var routes = _routes.Select(r => r.CreateConfiguration()).ToList();

        Configuration.Routes = routes;

        return Configuration;
    }
}
