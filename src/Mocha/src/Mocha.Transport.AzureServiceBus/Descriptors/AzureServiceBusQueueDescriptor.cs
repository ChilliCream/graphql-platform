using Mocha.Features;

namespace Mocha.Transport.AzureServiceBus;

internal sealed class AzureServiceBusQueueDescriptor
    : MessagingDescriptorBase<AzureServiceBusQueueDescriptorConfiguration>
    , IAzureServiceBusQueueDescriptor
{
    private AzureServiceBusQueueDescriptor(IMessagingConfigurationContext context, string name)
        : base(context)
    {
        Configuration = new AzureServiceBusQueueDescriptorConfiguration(name);
    }

    protected internal override AzureServiceBusQueueDescriptorConfiguration Configuration { get; protected set; }

    public IAzureServiceBusQueueDescriptor AutoDelete(bool autoDelete = true)
    {
        Configuration.Queue.AutoDelete = autoDelete;
        return this;
    }

    public IAzureServiceBusQueueDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.Queue.AutoProvision = autoProvision;
        return this;
    }

    public IAzureServiceBusQueueDescriptor WithAutoDeleteOnIdle(TimeSpan autoDeleteOnIdle)
    {
        Configuration.Queue.AutoDeleteOnIdle = autoDeleteOnIdle;
        return this;
    }

    public IAzureServiceBusQueueDescriptor WithLockDuration(TimeSpan lockDuration)
    {
        Configuration.Queue.LockDuration = lockDuration;
        return this;
    }

    public IAzureServiceBusQueueDescriptor WithMaxDeliveryCount(int maxDeliveryCount)
    {
        Configuration.Queue.MaxDeliveryCount = maxDeliveryCount;
        return this;
    }

    public IAzureServiceBusQueueDescriptor WithDefaultMessageTimeToLive(TimeSpan defaultMessageTimeToLive)
    {
        Configuration.Queue.DefaultMessageTimeToLive = defaultMessageTimeToLive;
        return this;
    }

    public IAzureServiceBusQueueDescriptor WithMaxSizeInMegabytes(long maxSizeInMegabytes)
    {
        Configuration.Queue.MaxSizeInMegabytes = maxSizeInMegabytes;
        return this;
    }

    public IAzureServiceBusQueueDescriptor WithRequiresSession(bool requiresSession = true)
    {
        Configuration.Queue.RequiresSession = requiresSession;
        return this;
    }

    public IAzureServiceBusQueueDescriptor WithEnablePartitioning(bool enablePartitioning = true)
    {
        Configuration.Queue.EnablePartitioning = enablePartitioning;
        return this;
    }

    public IAzureServiceBusQueueDescriptor WithForwardTo(string entityName)
    {
        Configuration.Queue.ForwardTo = entityName;
        return this;
    }

    public IAzureServiceBusQueueDescriptor WithForwardDeadLetteredMessagesTo(string entityName)
    {
        Configuration.Queue.ForwardDeadLetteredMessagesTo = entityName;
        return this;
    }

    public IAzureServiceBusQueueDescriptor WithDeadLetteringOnMessageExpiration(
        bool deadLetteringOnMessageExpiration = true)
    {
        Configuration.Queue.DeadLetteringOnMessageExpiration = deadLetteringOnMessageExpiration;
        return this;
    }

    public IAzureServiceBusQueueDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        Configuration.ConsumerIdentities.Add(typeof(THandler));
        return this;
    }

    public IAzureServiceBusQueueDescriptor Handler(Type handlerType)
    {
        ArgumentNullException.ThrowIfNull(handlerType);
        Configuration.ConsumerIdentities.Add(handlerType);
        return this;
    }

    public IAzureServiceBusQueueDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        Configuration.ConsumerIdentities.Add(typeof(TConsumer));
        return this;
    }

    public IAzureServiceBusQueueDescriptor Consumer(Type consumerType)
    {
        ArgumentNullException.ThrowIfNull(consumerType);
        Configuration.ConsumerIdentities.Add(consumerType);
        return this;
    }

    public IAzureServiceBusQueueDescriptor Receives<TMessage>()
    {
        Configuration.ReceivedMessageTypes.Add(typeof(TMessage));
        return this;
    }

    public IAzureServiceBusQueueDescriptor Receives(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        Configuration.ReceivedMessageTypes.Add(messageType);
        return this;
    }

    public IAzureServiceBusQueueDescriptor BindImplicitly()
    {
        Configuration.BindMode = MessagingBindMode.Implicit;
        return this;
    }

    public IAzureServiceBusQueueDescriptor BindExplicitly()
    {
        Configuration.BindMode = MessagingBindMode.Explicit;
        return this;
    }

    public IAzureServiceBusQueueDescriptor Kind(ReceiveEndpointKind kind)
    {
        Configuration.Kind = kind;
        return this;
    }

    public IAzureServiceBusQueueDescriptor MaxConcurrency(int maxConcurrency)
    {
        Configuration.MaxConcurrency = maxConcurrency;
        return this;
    }

    public IAzureServiceBusQueueDescriptor PrefetchCount(int? count)
    {
        Configuration.PrefetchCount = count;
        return this;
    }

    public IAzureServiceBusQueueDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        if (before is not null && after is not null)
        {
            throw new InvalidOperationException("The before and after arguments cannot both be specified.");
        }

        if (before is null && after is null)
        {
            Configuration.ReceiveMiddlewares.Add(configuration);
        }
        else if (before is not null)
        {
            Configuration.ReceivePipelineModifiers.Prepend(configuration, before);
        }
        else
        {
            Configuration.ReceivePipelineModifiers.Append(configuration, after);
        }

        return this;
    }

    public IAzureServiceBusQueueDescriptor FaultEndpoint(Uri address)
    {
        EnsureAbsoluteAddress(address);
        var feature = Configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
        feature.Address = address;
        feature.IsDisabled = false;
        return this;
    }

    /// <inheritdoc />
    [Obsolete("Use FaultEndpoint(Uri) instead.")]
    public IAzureServiceBusQueueDescriptor FaultEndpoint(string address)
        => FaultEndpoint(new Uri(address, UriKind.Absolute));

    public IAzureServiceBusQueueDescriptor DisableFaultEndpoint()
    {
        var feature = Configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
        feature.IsDisabled = true;
        feature.Address = null;
        return this;
    }

    public IAzureServiceBusQueueDescriptor SkippedEndpoint(Uri address)
    {
        EnsureAbsoluteAddress(address);
        var feature = Configuration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
        feature.Address = address;
        feature.IsDisabled = false;
        return this;
    }

    /// <inheritdoc />
    [Obsolete("Use SkippedEndpoint(Uri) instead.")]
    public IAzureServiceBusQueueDescriptor SkippedEndpoint(string address)
        => SkippedEndpoint(new Uri(address, UriKind.Absolute));

    public IAzureServiceBusQueueDescriptor DisableSkippedEndpoint()
    {
        var feature = Configuration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
        feature.IsDisabled = true;
        feature.Address = null;
        return this;
    }

    public IAzureServiceBusQueueDescriptor UseNativeDeadLetterForwarding()
    {
        Configuration.UseNativeDeadLetterForwarding = true;
        return this;
    }

    public IAzureServiceBusQueueDescriptor WithMaxConcurrentSessions(int maxConcurrentSessions)
    {
        Configuration.MaxConcurrentSessions = maxConcurrentSessions;
        return this;
    }

    public IAzureServiceBusQueueDescriptor WithMaxConcurrentCallsPerSession(int maxConcurrentCallsPerSession)
    {
        Configuration.MaxConcurrentCallsPerSession = maxConcurrentCallsPerSession;
        return this;
    }

    public IAzureServiceBusQueueDescriptor WithSessionIdleTimeout(TimeSpan sessionIdleTimeout)
    {
        Configuration.SessionIdleTimeout = sessionIdleTimeout;
        return this;
    }

    public IAzureServiceBusQueueDescriptor WithMaxAutoLockRenewalDuration(TimeSpan maxAutoLockRenewalDuration)
    {
        Configuration.MaxAutoLockRenewalDuration = maxAutoLockRenewalDuration;
        return this;
    }

    public IAzureServiceBusQueueDescriptor BindFrom(Uri source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Configuration.SourceTopics.Add(source);
        return this;
    }

    public AzureServiceBusQueueDescriptorConfiguration CreateConfiguration() => Configuration;

    public static AzureServiceBusQueueDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);

    private static void EnsureAbsoluteAddress(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsAbsoluteUri)
        {
            throw new ArgumentException("The endpoint address must be an absolute URI.", nameof(address));
        }
    }
}
