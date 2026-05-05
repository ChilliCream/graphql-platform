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
    public new IAzureServiceBusReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        base.Consumer<TConsumer>();

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
    public new IAzureServiceBusReceiveEndpointDescriptor FaultEndpoint(string name)
    {
        base.FaultEndpoint(name);

        return this;
    }

    /// <inheritdoc />
    public new IAzureServiceBusReceiveEndpointDescriptor SkippedEndpoint(string name)
    {
        base.SkippedEndpoint(name);

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
