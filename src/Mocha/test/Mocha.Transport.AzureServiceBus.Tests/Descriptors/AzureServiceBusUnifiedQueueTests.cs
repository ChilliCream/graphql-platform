using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;
using Mocha.Transport.AzureServiceBus.Tests.Topology;

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

        var endpoint = transport
            .ReceiveEndpoints.OfType<AzureServiceBusReceiveEndpoint>()
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
            t.Queue("orders").MaxDeliveryCount(7);
            t.Endpoint("orders").PrefetchCount(23);
            t.DeclareQueue("orders").AutoProvision(false);
        });
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;
        var queue = topology.Queues.Single(q => q.Name == "orders");
        var endpoint = configuration
            .ReceiveEndpoints.OfType<AzureServiceBusReceiveEndpointConfiguration>()
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
                .RequiresSession()
                .LockDuration(TimeSpan.FromSeconds(45))
                .MaxConcurrentSessions(4)
                .MaxConcurrentCallsPerSession(2)
                .SessionIdleTimeout(TimeSpan.FromSeconds(30))
                .MaxAutoLockRenewalDuration(TimeSpan.FromMinutes(3))
                .PrefetchCount(17);
        });
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;

        var queue = topology.Queues.Single(q => q.Name == "sessions");
        var azureConfiguration =
            Assert.IsType<AzureServiceBusReceiveEndpointConfiguration>(configuration.ReceiveEndpoints.Single());

        Assert.True(queue.RequiresSession);
        Assert.Equal(TimeSpan.FromSeconds(45), queue.LockDuration);
        Assert.Equal(4, azureConfiguration.MaxConcurrentSessions);
        Assert.Equal(2, azureConfiguration.MaxConcurrentCallsPerSession);
        Assert.Equal(TimeSpan.FromSeconds(30), azureConfiguration.SessionIdleTimeout);
        Assert.Equal(TimeSpan.FromMinutes(3), azureConfiguration.MaxAutoLockRenewalDuration);
        Assert.Equal(17, azureConfiguration.PrefetchCount);
    }

    [Fact]
    public void Queue_Should_PropagateTemporaryToEndpointAndQueue_When_ConfiguredWithDefaultIdleTimeout()
    {
        var (transport, configuration) = CreateTransport(t =>
        {
            t.BindExplicitly();
            t.Queue("audit").Temporary();
        });
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;
        var queue = topology.Queues.Single(q => q.Name == "audit");
        var endpoint = configuration
            .ReceiveEndpoints.OfType<AzureServiceBusReceiveEndpointConfiguration>()
            .Single(e => e.Name == "audit");

        Assert.True(endpoint.IsTemporary);
        Assert.Equal(TimeSpan.FromHours(24), queue.AutoDeleteOnIdle);
    }

    [Fact]
    public void Queue_Should_PropagateTemporaryIdleTimeoutToQueue_When_ConfiguredWithExplicitIdleTimeout()
    {
        var (transport, _) = CreateTransport(t =>
        {
            t.BindExplicitly();
            t.Queue("audit").Temporary(TimeSpan.FromMinutes(15));
        });
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;
        var queue = topology.Queues.Single(q => q.Name == "audit");

        Assert.Equal(TimeSpan.FromMinutes(15), queue.AutoDeleteOnIdle);
    }

    [Fact]
    public void Queue_Should_Throw_When_TemporaryIdleTimeoutBelowMinimum()
    {
        // arrange
        Action action = () =>
            CreateTransport(t =>
            {
                t.BindExplicitly();
                t.Queue("audit").Temporary(TimeSpan.FromMinutes(1));
            });

        // act & assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
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
            t.Queue("orders").FaultEndpoint(new Uri("queue:custom-error")).DisableSkippedEndpoint();
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
    public void Queue_Should_RejectAutoForwardingSource_When_NoConsumerAttached()
    {
        // arrange
        Action action = () =>
            CreateTransport(t =>
            {
                t.BindExplicitly();
                t.Queue("forwarding-source").ForwardTo("forwarding-target");
            });

        // act
        var exception = Assert.Throws<InvalidOperationException>(action);

        // assert
        Assert.Contains("DeclareQueue", exception.Message);
    }

    [Fact]
    public void Queue_Should_RejectAutoForwardingSource_When_ConsumerAttached()
    {
        // arrange
        Action action = () =>
        {
            var services = new ServiceCollection();
            services
                .AddMessageBus()
                .AddConsumer<OrderConsumer>()
                .AddAzureServiceBus(t =>
                {
                    t.ConnectionString(DummyConnectionString);
                    t.BindExplicitly();
                    t.Queue("forwarding-consumer-source")
                        .ForwardTo("forwarding-consumer-target")
                        .Consumer<OrderConsumer>();
                })
                .BuildRuntime();
        };

        // act
        var exception = Assert.Throws<InvalidOperationException>(action);

        // assert
        Assert.Equal(
            "Receive endpoint 'forwarding-consumer-source' cannot target queue 'forwarding-consumer-source' "
                + "configured with auto-forwarding (ForwardTo). The broker rejects receivers on an "
                + "auto-forwarding source. Declare forwarding-only queues with DeclareQueue instead of "
                + "Queue or Endpoint.",
            exception.Message);
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
                configuration = ((IMessagingDescriptor<AzureServiceBusTransportConfiguration>)t).Extend().Configuration;
            })
            .BuildRuntime();

        var endpoints = configuration!
            .ReceiveEndpoints.Where(e => e.ConsumerIdentities.Contains(typeof(OrderConsumer)))
            .ToList();

        var endpoint = Assert.Single(endpoints);
        Assert.Equal("orders", endpoint.Name);
    }

    [Fact]
    public void QueueEndpointMerge_Should_PreferExplicitEndpointMaxConcurrency_When_QueueAlsoSetsIt()
    {
        // arrange
        var (_, configuration) = CreateTransport(t =>
        {
            t.BindExplicitly();
            t.Queue("orders").MaxConcurrency(2);
            t.Endpoint("orders").MaxConcurrency(9);
        });

        // act
        var endpoint = configuration.ReceiveEndpoints.Single();

        // assert
        Assert.Equal(9, endpoint.MaxConcurrency);
    }

    [Fact]
    public void QueueEndpointMerge_Should_PreferExplicitEndpointPrefetchCount_When_QueueAlsoSetsIt()
    {
        // arrange
        var (_, configuration) = CreateTransport(t =>
        {
            t.BindExplicitly();
            t.Queue("orders").PrefetchCount(2);
            t.Endpoint("orders").PrefetchCount(9);
        });

        // act
        var endpoint = Assert.IsType<AzureServiceBusReceiveEndpointConfiguration>(configuration.ReceiveEndpoints.Single());

        // assert
        Assert.Equal(9, endpoint.PrefetchCount);
    }

    [Fact]
    public void QueueEndpointMerge_Should_PreferExplicitEndpointFaultEndpoint_When_QueueAlsoSetsIt()
    {
        // arrange
        var (_, configuration) = CreateTransport(t =>
        {
            t.BindExplicitly();
            t.Queue("orders").FaultEndpoint(new Uri("queue:from-queue-error"));
            t.Endpoint("orders").FaultEndpoint(new Uri("queue:from-endpoint-error"));
        });

        // act
        var endpoint = configuration.ReceiveEndpoints.Single();
        var fault = endpoint.Features.Get<ReceiveFaultEndpointFeature>();

        // assert
        Assert.Equal("queue:from-endpoint-error", fault?.Address?.OriginalString);
    }

    [Fact]
    public void QueueEndpointMerge_Should_PreferExplicitEndpointSkippedEndpoint_When_QueueAlsoSetsIt()
    {
        // arrange
        var (_, configuration) = CreateTransport(t =>
        {
            t.BindExplicitly();
            t.Queue("orders").SkippedEndpoint(new Uri("queue:from-queue-skipped"));
            t.Endpoint("orders").SkippedEndpoint(new Uri("queue:from-endpoint-skipped"));
        });

        // act
        var endpoint = configuration.ReceiveEndpoints.Single();
        var skipped = endpoint.Features.Get<ReceiveSkippedEndpointFeature>();

        // assert
        Assert.Equal("queue:from-endpoint-skipped", skipped?.Address?.OriginalString);
    }

    [Fact]
    public void QueueEndpointMerge_Should_PreferExplicitEndpointDisabledFault_When_QueueSetsAddress()
    {
        // arrange
        // The endpoint explicitly disables the fault endpoint; the queue's address must not resurrect it.
        var (_, configuration) = CreateTransport(t =>
        {
            t.BindExplicitly();
            t.Queue("orders").FaultEndpoint(new Uri("queue:from-queue-error"));
            t.Endpoint("orders").DisableFaultEndpoint();
        });

        // act
        var endpoint = configuration.ReceiveEndpoints.Single();
        var fault = endpoint.Features.Get<ReceiveFaultEndpointFeature>();

        // assert
        Assert.True(fault?.IsDisabled);
        Assert.Null(fault?.Address);
    }

    [Fact]
    public void Describe_Should_ShowSameTopology_When_ConfigurationUsesNoUnifiedQueueApi()
    {
        // arrange
        // A configuration declared entirely through DeclareQueue()/Endpoint() (no Queue() API)
        // must flow through the unified queue lowering pass unchanged.
        var services = new ServiceCollection();
        var runtime = services
            .AddMessageBus()
            .AddConsumer<OrderConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(DummyConnectionString);
                t.BindExplicitly();
                t.DeclareQueue("orders").AutoProvision(true);
                t.Endpoint("orders").Consumer<OrderConsumer>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        AzureServiceBusDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Queue_Should_InheritAutoProvisionOnSubscription_When_QueueOptsIn()
    {
        // arrange
        // With the transport deny-by-default, a queue that opts in to provisioning must carry that
        // opt-in onto the subscription its BindFrom derives; the foreign source topic stays opted out.
        var (transport, _) = CreateTransport(t =>
        {
            t.AutoProvision(false);
            t.BindExplicitly();
            t.Queue("orders").AutoProvision(true).BindFrom(new Uri("topic:incoming-orders"));
        });
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;

        // act
        var subscription = topology.Subscriptions.Single();
        var topic = topology.Topics.Single(e => e.Name == "incoming-orders");

        // assert
        Assert.True(subscription.AutoProvision);
        Assert.Null(topic.AutoProvision);
    }

    [Fact]
    public void Queue_Should_LeaveSubscriptionAutoProvisionUnset_When_QueueDoesNotOptIn()
    {
        // arrange
        var (transport, _) = CreateTransport(t =>
        {
            t.BindExplicitly();
            t.Queue("orders").BindFrom(new Uri("topic:incoming-orders"));
        });
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;

        // act
        var subscription = topology.Subscriptions.Single();

        // assert
        Assert.Null(subscription.AutoProvision);
    }

    [Fact]
    public void Queue_Should_KeepExplicitSubscriptionAutoProvision_When_QueueOptsIn()
    {
        // arrange
        // DeclareSubscription is get-or-add, so an explicitly configured subscription must not be
        // overridden by the inheritance a later BindFrom applies.
        var (transport, _) = CreateTransport(t =>
        {
            t.AutoProvision(false);
            t.BindExplicitly();
            t.DeclareSubscription("incoming-orders", "orders").AutoProvision(false);
            t.Queue("orders").AutoProvision(true).BindFrom(new Uri("topic:incoming-orders"));
        });
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;

        // act
        var subscription = topology.Subscriptions.Single();

        // assert
        Assert.False(subscription.AutoProvision);
    }

    [Fact]
    public void Queue_Should_KeepSubscriptionOptOut_When_QueueOptsIn()
    {
        // arrange
        // A binding can opt out of provisioning individually even though its queue opts in.
        var (transport, _) = CreateTransport(t =>
        {
            t.AutoProvision(false);
            t.BindExplicitly();
            t.Queue("orders")
                .AutoProvision(true)
                .BindFrom(new Uri("topic:incoming-orders"), autoProvision: false);
        });
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;

        // act
        var subscription = topology.Subscriptions.Single();

        // assert
        Assert.False(subscription.AutoProvision);
    }

    [Fact]
    public void Queue_Should_ProvisionSubscription_When_OnlyBindingOptsIn()
    {
        // arrange
        // A binding can opt in individually even though its queue does not.
        var (transport, _) = CreateTransport(t =>
        {
            t.AutoProvision(false);
            t.BindExplicitly();
            t.Queue("orders").BindFrom(new Uri("topic:incoming-orders"), autoProvision: true);
        });
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;

        // act
        var subscription = topology.Subscriptions.Single();

        // assert
        Assert.True(subscription.AutoProvision);
    }

    [Fact]
    public void Queue_Should_InheritQueueOptOut_When_TransportProvisionsByDefault()
    {
        // arrange
        // Inheritance also carries an opt-out: a queue that is managed externally drags its
        // BindFrom subscription out of provisioning even though the transport provisions by default.
        var (transport, _) = CreateTransport(t =>
        {
            t.BindExplicitly();
            t.Queue("orders").AutoProvision(false).BindFrom(new Uri("topic:incoming-orders"));
        });
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;

        // act
        var subscription = topology.Subscriptions.Single();

        // assert
        Assert.False(subscription.AutoProvision);
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
                configuration = ((IMessagingDescriptor<AzureServiceBusTransportConfiguration>)t).Extend().Configuration;
            })
            .BuildRuntime();

        return (runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single(), configuration!);
    }

    private const string DummyConnectionString =
        "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test";

    public sealed class OrderMessage;

    public sealed class OrderConsumer : IConsumer<OrderMessage>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderMessage> context) => default;
    }
}
