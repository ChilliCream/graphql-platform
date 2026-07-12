using Mocha.Features;

namespace Mocha.Transport.InMemory;

/// <summary>
/// Descriptor for configuring an in-memory queue and its receive endpoint.
/// </summary>
internal sealed class InMemoryQueueDescriptor
    : MessagingDescriptorBase<InMemoryQueueDescriptorConfiguration>
    , IInMemoryQueueDescriptor
{
    private InMemoryQueueDescriptor(IMessagingConfigurationContext context, string name)
        : base(context)
    {
        Configuration = new InMemoryQueueDescriptorConfiguration(name);
    }

    protected internal override InMemoryQueueDescriptorConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        Configuration.ConsumerIdentities.Add(typeof(THandler));
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor Handler(Type handlerType)
    {
        Configuration.ConsumerIdentities.Add(handlerType);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        Configuration.ConsumerIdentities.Add(typeof(TConsumer));
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor Consumer(Type consumerType)
    {
        ArgumentNullException.ThrowIfNull(consumerType);
        Configuration.ConsumerIdentities.Add(consumerType);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor Receives<TMessage>()
    {
        Configuration.ReceivedMessageTypes.Add(typeof(TMessage));
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor Receives(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        Configuration.ReceivedMessageTypes.Add(messageType);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor BindImplicitly()
    {
        Configuration.BindMode = MessagingBindMode.Implicit;
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor BindExplicitly()
    {
        Configuration.BindMode = MessagingBindMode.Explicit;
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor MaxConcurrency(int maxConcurrency)
    {
        Configuration.MaxConcurrency = maxConcurrency;
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor Kind(ReceiveEndpointKind kind)
    {
        Configuration.Kind = kind;
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor UseReceive(
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
    public IInMemoryQueueDescriptor FaultEndpoint(Uri address)
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
    public IInMemoryQueueDescriptor DisableFaultEndpoint()
    {
        var feature = Configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
        feature.Address = null;
        feature.IsDisabled = true;
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor SkippedEndpoint(Uri address)
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
    public IInMemoryQueueDescriptor DisableSkippedEndpoint()
    {
        var feature = Configuration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
        feature.Address = null;
        feature.IsDisabled = true;
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor BindFrom(Uri source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Configuration.SourceBindings.Add(source);
        return this;
    }

    public InMemoryQueueDescriptorConfiguration CreateConfiguration() => Configuration;

    public static InMemoryQueueDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
