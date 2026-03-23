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
    public IConsumerDescriptor UseConsumer(
        ConsumerMiddlewareConfiguration configuration,
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
            Configuration.ConsumerMiddlewares.Add(configuration);
            return this;
        }

        if (before is not null)
        {
            Configuration.ConsumerPipelineModifiers.Prepend(configuration, before);
        }
        else
        {
            Configuration.ConsumerPipelineModifiers.Append(configuration, after);
        }

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
