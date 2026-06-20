using Mocha.Features;

namespace Mocha.Transport.Postgres;

/// <summary>
/// Descriptor for configuring a PostgreSQL queue and its receive endpoint.
/// </summary>
internal sealed class PostgresQueueDescriptor
    : MessagingDescriptorBase<PostgresQueueDescriptorConfiguration>
    , IPostgresQueueDescriptor
{
    private PostgresQueueDescriptor(IMessagingConfigurationContext context, string name)
        : base(context)
    {
        Configuration = new PostgresQueueDescriptorConfiguration(name);
    }

    protected internal override PostgresQueueDescriptorConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IPostgresQueueDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.Queue.AutoProvision = autoProvision;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor AutoDelete(bool autoDelete = true)
    {
        Configuration.Queue.AutoDelete = autoDelete;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        Configuration.ConsumerIdentities.Add(typeof(THandler));
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor Handler(Type handlerType)
    {
        Configuration.ConsumerIdentities.Add(handlerType);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        Configuration.ConsumerIdentities.Add(typeof(TConsumer));
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor Consumer(Type consumerType)
    {
        ArgumentNullException.ThrowIfNull(consumerType);
        Configuration.ConsumerIdentities.Add(consumerType);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor Receives<TMessage>()
    {
        Configuration.ReceivedMessageTypes.Add(typeof(TMessage));
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor Receives(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        Configuration.ReceivedMessageTypes.Add(messageType);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor BindImplicitly()
    {
        Configuration.BindMode = MessagingBindMode.Implicit;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor BindExplicitly()
    {
        Configuration.BindMode = MessagingBindMode.Explicit;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor MaxBatchSize(int size)
    {
        Configuration.MaxBatchSize = size;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor Kind(ReceiveEndpointKind kind)
    {
        Configuration.Kind = kind;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor MaxConcurrency(int maxConcurrency)
    {
        Configuration.MaxConcurrency = maxConcurrency;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor UseReceive(
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
    public IPostgresQueueDescriptor FaultEndpoint(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsAbsoluteUri)
        {
            throw new ArgumentException("The endpoint address must be an absolute URI.", nameof(address));
        }

        var feature = Configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
        feature.Address = address;
        feature.IsDisabled = false;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor DisableFaultEndpoint()
    {
        var feature = Configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
        feature.IsDisabled = true;
        feature.Address = null;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor SkippedEndpoint(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsAbsoluteUri)
        {
            throw new ArgumentException("The endpoint address must be an absolute URI.", nameof(address));
        }

        var feature = Configuration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
        feature.Address = address;
        feature.IsDisabled = false;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor DisableSkippedEndpoint()
    {
        var feature = Configuration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
        feature.IsDisabled = true;
        feature.Address = null;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor BindFrom(Uri source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Configuration.SourceBindings.Add(source);
        return this;
    }

    public PostgresQueueDescriptorConfiguration CreateConfiguration() => Configuration;

    public static PostgresQueueDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
