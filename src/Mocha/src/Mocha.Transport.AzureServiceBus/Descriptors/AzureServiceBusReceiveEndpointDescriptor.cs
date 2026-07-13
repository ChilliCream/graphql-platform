using Mocha.Features;

namespace Mocha.Transport.AzureServiceBus;

internal sealed class AzureServiceBusReceiveEndpointDescriptor
    : ReceiveEndpointDescriptor<AzureServiceBusReceiveEndpointConfiguration>
    , IAzureServiceBusReceiveEndpointDescriptor
{
    internal AzureServiceBusReceiveEndpointDescriptor(IMessagingConfigurationContext discoveryContext, string name)
        : base(discoveryContext)
    {
        Configuration = new AzureServiceBusReceiveEndpointConfiguration { Name = name, QueueName = name };
    }

    /// <inheritdoc />
    public new IAzureServiceBusReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        base.Handler<THandler>();

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusReceiveEndpointDescriptor Handler(Type handlerType)
    {
        base.Handler(handlerType);

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusReceiveEndpointDescriptor Consumer(Type consumerType)
    {
        base.Consumer(consumerType);

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        base.Consumer<TConsumer>();

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusReceiveEndpointDescriptor Receives<TMessage>()
    {
        base.Receives<TMessage>();

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusReceiveEndpointDescriptor Receives(Type messageType)
    {
        base.Receives(messageType);

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusReceiveEndpointDescriptor BindImplicitly()
    {
        base.BindImplicitly();

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusReceiveEndpointDescriptor BindExplicitly()
    {
        base.BindExplicitly();

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind)
    {
        base.Kind(kind);

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency)
    {
        base.MaxConcurrency(maxConcurrency);

        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusReceiveEndpointDescriptor Queue(string name)
    {
        Configuration.QueueName = name;

        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusReceiveEndpointDescriptor PrefetchCount(int? count)
    {
        Configuration.PrefetchCount = count;

        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusReceiveEndpointDescriptor FaultEndpoint(Uri address)
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
    [Obsolete("Use FaultEndpoint(Uri) instead.")]
    public IAzureServiceBusReceiveEndpointDescriptor FaultEndpoint(string address)
        => FaultEndpoint(new Uri(address, UriKind.Absolute));

    /// <inheritdoc />
    public IAzureServiceBusReceiveEndpointDescriptor DisableFaultEndpoint()
    {
        var feature = Configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
        feature.Address = null;
        feature.IsDisabled = true;

        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusReceiveEndpointDescriptor SkippedEndpoint(Uri address)
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
    [Obsolete("Use SkippedEndpoint(Uri) instead.")]
    public IAzureServiceBusReceiveEndpointDescriptor SkippedEndpoint(string address)
        => SkippedEndpoint(new Uri(address, UriKind.Absolute));

    /// <inheritdoc />
    public IAzureServiceBusReceiveEndpointDescriptor DisableSkippedEndpoint()
    {
        var feature = Configuration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
        feature.Address = null;
        feature.IsDisabled = true;

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusReceiveEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseReceive(configuration, before: before, after: after);

        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusReceiveEndpointDescriptor UseNativeDeadLetterForwarding()
    {
        Configuration.UseNativeDeadLetterForwarding = true;

        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusReceiveEndpointDescriptor WithMaxConcurrentSessions(int maxConcurrentSessions)
    {
        Configuration.MaxConcurrentSessions = maxConcurrentSessions;

        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusReceiveEndpointDescriptor WithMaxConcurrentCallsPerSession(int maxConcurrentCallsPerSession)
    {
        Configuration.MaxConcurrentCallsPerSession = maxConcurrentCallsPerSession;

        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusReceiveEndpointDescriptor WithSessionIdleTimeout(TimeSpan sessionIdleTimeout)
    {
        Configuration.SessionIdleTimeout = sessionIdleTimeout;

        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusReceiveEndpointDescriptor WithMaxAutoLockRenewalDuration(TimeSpan maxAutoLockRenewalDuration)
    {
        Configuration.MaxAutoLockRenewalDuration = maxAutoLockRenewalDuration;

        return this;
    }

    public AzureServiceBusReceiveEndpointConfiguration CreateConfiguration()
    {
        return Configuration;
    }

    public static AzureServiceBusReceiveEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
