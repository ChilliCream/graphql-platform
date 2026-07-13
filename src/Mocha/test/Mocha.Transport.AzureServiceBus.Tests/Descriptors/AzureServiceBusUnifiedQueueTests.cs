using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Descriptors;

public class AzureServiceBusUnifiedQueueTests
{
    [Fact]
    public void Queue_Should_MaterializeEndpointAndTopologyEntity_When_NoConsumerAttached()
    {
        var (transport, _) = CreateTransport(t =>
        {
            t.BindExplicitly();
            t.Queue("audit");
        });
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;

        var endpoint = transport.ReceiveEndpoints
            .OfType<AzureServiceBusReceiveEndpoint>()
            .Single(e => e.Name == "audit");
        var queue = topology.Queues.Single(q => q.Name == "audit");

        Assert.Equal("audit", endpoint.Name);
        Assert.Equal("audit", endpoint.Queue.Name);
        Assert.Equal("audit", queue.Name);
        Assert.Equal(TopologyOrigin.Declared, queue.Origin);
    }

    [Fact]
    public void QueueEndpointDeclareQueue_Should_ConvergeToOneEntity_When_NameIsShared()
    {
        var (transport, configuration) = CreateTransport(t =>
        {
            t.BindExplicitly();
            t.Queue("orders").WithMaxDeliveryCount(7);
            t.Endpoint("orders").PrefetchCount(23);
            t.DeclareQueue("orders").AutoProvision(false);
        });
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;
        var queue = topology.Queues.Single(q => q.Name == "orders");
        var endpoint = configuration.ReceiveEndpoints
            .OfType<AzureServiceBusReceiveEndpointConfiguration>()
            .Single(e => e.Name == "orders");

        Assert.Equal(7, queue.MaxDeliveryCount);
        Assert.False(queue.AutoProvision);
        Assert.Equal(23, endpoint.PrefetchCount);
    }

    [Fact]
    public void Queue_Should_LowerSessionAndEndpointOptions_When_Configured()
    {
        var (transport, configuration) = CreateTransport(t =>
        {
            t.BindExplicitly();
            t.Queue("sessions")
                .WithRequiresSession()
                .WithLockDuration(TimeSpan.FromSeconds(45))
                .WithMaxConcurrentSessions(4)
                .WithMaxConcurrentCallsPerSession(2)
                .WithSessionIdleTimeout(TimeSpan.FromSeconds(30))
                .WithMaxAutoLockRenewalDuration(TimeSpan.FromMinutes(3))
                .PrefetchCount(17);
        });
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;

        var queue = topology.Queues.Single(q => q.Name == "sessions");
        var azureConfiguration = Assert.IsType<AzureServiceBusReceiveEndpointConfiguration>(
            configuration.ReceiveEndpoints.Single());

        Assert.True(queue.RequiresSession);
        Assert.Equal(TimeSpan.FromSeconds(45), queue.LockDuration);
        Assert.Equal(4, azureConfiguration.MaxConcurrentSessions);
        Assert.Equal(2, azureConfiguration.MaxConcurrentCallsPerSession);
        Assert.Equal(TimeSpan.FromSeconds(30), azureConfiguration.SessionIdleTimeout);
        Assert.Equal(TimeSpan.FromMinutes(3), azureConfiguration.MaxAutoLockRenewalDuration);
        Assert.Equal(17, azureConfiguration.PrefetchCount);
    }

    [Fact]
    public void Queue_Should_DeclareExplicitSubscription_When_BindFromTopicConfigured()
    {
        var (transport, _) = CreateTransport(t =>
        {
            t.BindExplicitly();
            t.Queue("orders").BindFrom(new Uri("topic:incoming-orders"));
        });
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;

        var subscription = topology.Subscriptions.Single();

        Assert.Equal("incoming-orders", subscription.Source.Name);
        Assert.Equal("orders", subscription.Destination.Name);
        Assert.Equal(TopologyOrigin.Declared, subscription.Origin);
    }

    [Fact]
    public void Queue_Should_CopyFaultAndSkippedFeatures_When_Configured()
    {
        var (_, configuration) = CreateTransport(t =>
        {
            t.BindExplicitly();
            t.Queue("orders")
                .FaultEndpoint(new Uri("queue:custom-error"))
                .DisableSkippedEndpoint();
        });
        var endpoint = configuration.ReceiveEndpoints.Single();
        var fault = endpoint.Features.Get<ReceiveFaultEndpointFeature>();
        var skipped = endpoint.Features.Get<ReceiveSkippedEndpointFeature>();

        Assert.Equal("queue:custom-error", fault?.Address?.OriginalString);
        Assert.False(fault?.IsDisabled);
        Assert.Null(skipped?.Address);
        Assert.True(skipped?.IsDisabled);
    }

    [Fact]
    public void CompatibilityAliases_Should_ForwardToCanonicalConfiguration()
    {
#pragma warning disable CS0618
        var (_, configuration) = CreateTransport(t =>
        {
            t.BindHandlersImplicitly();
            t.BindHandlersExplicitly();
            t.Endpoint("orders")
                .FaultEndpoint("queue:custom-error")
                .SkippedEndpoint("queue:custom-skipped");
        });
#pragma warning restore CS0618
        var endpoint = configuration.ReceiveEndpoints.Single();

        Assert.Equal(
            "queue:custom-error",
            endpoint.Features.Get<ReceiveFaultEndpointFeature>()?.Address?.OriginalString);
        Assert.Equal(
            "queue:custom-skipped",
            endpoint.Features.Get<ReceiveSkippedEndpointFeature>()?.Address?.OriginalString);
    }

    [Fact]
    public void Queue_Should_ClaimConsumerOnNamedEndpoint_When_BindingIsExplicit()
    {
        var services = new ServiceCollection();
        AzureServiceBusTransportConfiguration? configuration = null;
        var runtime = services
            .AddMessageBus()
            .AddConsumer<OrderConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(DummyConnectionString);
                t.BindExplicitly();
                t.Queue("orders").Consumer<OrderConsumer>();
                configuration = ((IMessagingDescriptor<AzureServiceBusTransportConfiguration>)t)
                    .Extend()
                    .Configuration;
            })
            .BuildRuntime();

        var endpoints = configuration!.ReceiveEndpoints
            .Where(e => e.ConsumerIdentities.Contains(typeof(OrderConsumer)))
            .ToList();

        var endpoint = Assert.Single(endpoints);
        Assert.Equal("orders", endpoint.Name);
    }

    private static (
        AzureServiceBusMessagingTransport Transport,
        AzureServiceBusTransportConfiguration Configuration) CreateTransport(
        Action<IAzureServiceBusMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        AzureServiceBusTransportConfiguration? configuration = null;
        var runtime = services
            .AddMessageBus()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(DummyConnectionString);
                configure(t);
                configuration = ((IMessagingDescriptor<AzureServiceBusTransportConfiguration>)t)
                    .Extend()
                    .Configuration;
            })
            .BuildRuntime();

        return (
            runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single(),
            configuration!);
    }

    private const string DummyConnectionString =
        "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test";

    public sealed class OrderMessage;

    public sealed class OrderConsumer : IConsumer<OrderMessage>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderMessage> context) => default;
    }
}
