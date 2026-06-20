using Mocha.Features;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Descriptor for configuring a RabbitMQ queue and its optional receive endpoint.
/// </summary>
internal sealed class RabbitMQQueueDescriptor
    : MessagingDescriptorBase<RabbitMQQueueDescriptorConfiguration>
    , IRabbitMQQueueDescriptor
{
    private RabbitMQQueueDescriptor(IMessagingConfigurationContext context, string name)
        : base(context)
    {
        Configuration = new RabbitMQQueueDescriptorConfiguration(name);
    }

    protected internal override RabbitMQQueueDescriptorConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor Durable(bool durable = true)
    {
        Configuration.Queue.Durable = durable;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor Quorum()
    {
        Configuration.Queue.Arguments ??= new Dictionary<string, object>();
        Configuration.Queue.Arguments["x-queue-type"] = RabbitMQQueueType.Quorum;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor WithArgument(string key, object value)
    {
        Configuration.Queue.Arguments ??= new Dictionary<string, object>();
        Configuration.Queue.Arguments[key] = value;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.Queue.AutoProvision = autoProvision;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        Configuration.ConsumerIdentities.Add(typeof(THandler));
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor Handler(Type handlerType)
    {
        Configuration.ConsumerIdentities.Add(handlerType);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        Configuration.ConsumerIdentities.Add(typeof(TConsumer));
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor Consumer(Type consumerType)
    {
        ArgumentNullException.ThrowIfNull(consumerType);
        Configuration.ConsumerIdentities.Add(consumerType);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor Receives<TMessage>()
    {
        Configuration.ReceivedMessageTypes.Add(typeof(TMessage));
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor Receives(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        Configuration.ReceivedMessageTypes.Add(messageType);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor BindImplicitly()
    {
        Configuration.BindMode = MessagingBindMode.Implicit;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor BindExplicitly()
    {
        Configuration.BindMode = MessagingBindMode.Explicit;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor MaxPrefetch(ushort maxPrefetch)
    {
        Configuration.MaxPrefetch = maxPrefetch;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor Kind(ReceiveEndpointKind kind)
    {
        Configuration.Kind = kind;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor MaxConcurrency(int maxConcurrency)
    {
        Configuration.MaxConcurrency = maxConcurrency;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        if (before is not null && after is not null)
        {
            throw ThrowHelper.BeforeAndAfterConflict();
        }

        if (before is null && after is null)
        {
            Configuration.ReceiveMiddlewares.Add(configuration);
            return this;
        }

        if (before is not null)
        {
            Configuration.ReceivePipelineModifiers.Prepend(configuration, before);
        }
        else
        {
            Configuration.ReceivePipelineModifiers.Append(configuration, after);
        }

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor FaultEndpoint(string name)
    {
        var feature = Configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
        feature.Address = new Uri(name);
        feature.IsDisabled = false;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor SkippedEndpoint(string name)
    {
        var feature = Configuration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
        feature.Address = new Uri(name);
        feature.IsDisabled = false;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor ErrorQueue(string name)
    {
        var feature = Configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
        feature.IsDisabled = false;
        feature.Address = QueueAddress(name);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor DisableErrorQueue()
    {
        var feature = Configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
        feature.IsDisabled = true;
        feature.Address = null;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor SkippedQueue(string name)
    {
        var feature = Configuration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
        feature.IsDisabled = false;
        feature.Address = QueueAddress(name);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor DisableSkippedQueue()
    {
        var feature = Configuration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
        feature.IsDisabled = true;
        feature.Address = null;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor BindFrom(Uri source, string? routingKey = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        Configuration.SourceBindings.Add(
            new RabbitMQQueueSourceBindingConfiguration { Source = source, RoutingKey = routingKey });
        return this;
    }

    public RabbitMQQueueDescriptorConfiguration CreateConfiguration() => Configuration;

    public static RabbitMQQueueDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);

    private static Uri QueueAddress(string name) => new($"queue:{name}");
}
