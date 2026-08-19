using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Descriptors;

/// <summary>
/// Proves that every public option on the transport, dispatch endpoint, receive endpoint, queue,
/// topic, and subscription descriptors reaches the materialized configuration object the runtime
/// consumes.
/// </summary>
public class AzureServiceBusDescriptorOptionsTests
{
    private const string DummyConnectionString =
        "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test";

    [Fact]
    public void TransportDescriptor_Should_MaterializeOptions_When_Configured()
    {
        // arrange & act
        AzureServiceBusTransportConfiguration? configuration = null;
        var services = new ServiceCollection();
        services
            .AddMessageBus()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(DummyConnectionString);
                t.AdministrationConnectionString("Endpoint=sb://admin/;SharedAccessKeyName=a;SharedAccessKey=b");
                t.AutoProvision(false);
                t.Schema("customsb");
                t.Name("custom-transport");
                t.BindExplicitly();
                configuration = ((IMessagingDescriptor<AzureServiceBusTransportConfiguration>)t).Extend().Configuration;
            })
            .BuildRuntime();

        // assert
        new
        {
            configuration!.ConnectionString,
            configuration.AdministrationConnectionString,
            configuration.AutoProvision,
            configuration.Schema,
            configuration.Name,
            BindMode = configuration.BindMode.ToString()
        }.MatchInlineSnapshot(
            """
            {
              "ConnectionString": "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test",
              "AdministrationConnectionString": "Endpoint=sb://admin/;SharedAccessKeyName=a;SharedAccessKey=b",
              "AutoProvision": false,
              "Schema": "customsb",
              "Name": "custom-transport",
              "BindMode": "Explicit"
            }
            """);
    }

    [Fact]
    public void DispatchEndpointDescriptor_Should_MaterializeOptions_When_Configured()
    {
        // arrange & act
        AzureServiceBusTransportConfiguration? configuration = null;
        var services = new ServiceCollection();
        services
            .AddMessageBus()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(DummyConnectionString);
                t.BindExplicitly();
                t.DeclareTopic("out-topic");
                t.DispatchEndpoint("out").ToQueue("out-queue").ToTopic("out-topic").Publish<OrderMessage>();
                configuration = ((IMessagingDescriptor<AzureServiceBusTransportConfiguration>)t).Extend().Configuration;
            })
            .BuildRuntime();
        var dispatch = configuration!.DispatchEndpoints.OfType<AzureServiceBusDispatchEndpointConfiguration>()
            .Single(e => e.Name == "out");

        // assert - ToQueue and ToTopic are mutually exclusive; the later call wins and clears the other.
        new
        {
            dispatch.Name,
            dispatch.QueueName,
            dispatch.TopicName
        }.MatchInlineSnapshot(
            """
            {
              "Name": "out",
              "QueueName": null,
              "TopicName": "out-topic"
            }
            """);
    }

    [Fact]
    public void ReceiveEndpointDescriptor_Should_MaterializeOptions_When_Configured()
    {
        // arrange & act
        var runtime = CreateRuntime(t =>
        {
            t.BindExplicitly();
            t.DeclareQueue("sessions-queue").RequiresSession();
            t.Endpoint("sessions")
                .Queue("sessions-queue")
                .PrefetchCount(23)
                .MaxConcurrency(9)
                .FaultEndpoint(new Uri("queue:sessions-error"))
                .SkippedEndpoint(new Uri("queue:sessions-skipped"))
                .UseNativeDeadLetterForwarding()
                .MaxConcurrentSessions(4)
                .MaxConcurrentCallsPerSession(2)
                .SessionIdleTimeout(TimeSpan.FromSeconds(30))
                .MaxAutoLockRenewalDuration(TimeSpan.FromMinutes(3));
        });
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var endpoint = transport.ReceiveEndpoints.OfType<AzureServiceBusReceiveEndpoint>()
            .Single(e => e.Name == "sessions");

        // assert
        new
        {
            endpoint.Configuration.QueueName,
            endpoint.Configuration.PrefetchCount,
            endpoint.Configuration.MaxConcurrency,
            endpoint.Configuration.UseNativeDeadLetterForwarding,
            endpoint.Configuration.MaxConcurrentSessions,
            endpoint.Configuration.MaxConcurrentCallsPerSession,
            SessionIdleTimeout = endpoint.Configuration.SessionIdleTimeout.ToString(),
            MaxAutoLockRenewalDuration = endpoint.Configuration.MaxAutoLockRenewalDuration.ToString()
        }.MatchInlineSnapshot(
            """
            {
              "QueueName": "sessions-queue",
              "PrefetchCount": 23,
              "MaxConcurrency": 9,
              "UseNativeDeadLetterForwarding": true,
              "MaxConcurrentSessions": 4,
              "MaxConcurrentCallsPerSession": 2,
              "SessionIdleTimeout": "00:00:30",
              "MaxAutoLockRenewalDuration": "00:03:00"
            }
            """);
    }

    [Fact]
    public void QueueTopologyDescriptor_Should_MaterializeOptions_When_Configured()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareQueue("archive");
            t.DeclareQueue("orders")
                .AutoProvision(true)
                .AutoDeleteOnIdle(TimeSpan.FromMinutes(10))
                .LockDuration(TimeSpan.FromSeconds(45))
                .MaxDeliveryCount(7)
                .DefaultMessageTimeToLive(TimeSpan.FromHours(2))
                .MaxSizeInMegabytes(2048)
                .EnablePartitioning(true)
                .ForwardTo("archive")
                .ForwardDeadLetteredMessagesTo("archive")
                .DeadLetteringOnMessageExpiration(true);
        });
        var queue = topology.Queues.Single(q => q.Name == "orders");

        // assert
        new
        {
            queue.AutoProvision,
            AutoDeleteOnIdle = queue.AutoDeleteOnIdle.ToString(),
            LockDuration = queue.LockDuration.ToString(),
            queue.MaxDeliveryCount,
            DefaultMessageTimeToLive = queue.DefaultMessageTimeToLive.ToString(),
            queue.MaxSizeInMegabytes,
            queue.EnablePartitioning,
            queue.ForwardTo,
            queue.ForwardDeadLetteredMessagesTo,
            queue.DeadLetteringOnMessageExpiration
        }.MatchInlineSnapshot(
            """
            {
              "AutoProvision": true,
              "AutoDeleteOnIdle": "00:10:00",
              "LockDuration": "00:00:45",
              "MaxDeliveryCount": 7,
              "DefaultMessageTimeToLive": "02:00:00",
              "MaxSizeInMegabytes": 2048,
              "EnablePartitioning": true,
              "ForwardTo": "archive",
              "ForwardDeadLetteredMessagesTo": "archive",
              "DeadLetteringOnMessageExpiration": true
            }
            """);
    }

    [Fact]
    public void TopicDescriptor_Should_MaterializeOptions_When_Configured()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareTopic("orders")
                .AutoProvision(true)
                .DefaultMessageTimeToLive(TimeSpan.FromHours(2))
                .MaxSizeInMegabytes(4096)
                .EnablePartitioning(true)
                .RequiresDuplicateDetection(true)
                .DuplicateDetectionHistoryTimeWindow(TimeSpan.FromMinutes(10))
                .AutoDeleteOnIdle(TimeSpan.FromHours(1))
                .SupportOrdering(false);
        });
        var topic = topology.Topics.Single(t => t.Name == "orders");

        // assert
        new
        {
            topic.AutoProvision,
            DefaultMessageTimeToLive = topic.DefaultMessageTimeToLive.ToString(),
            topic.MaxSizeInMegabytes,
            topic.EnablePartitioning,
            topic.RequiresDuplicateDetection,
            DuplicateDetectionHistoryTimeWindow = topic.DuplicateDetectionHistoryTimeWindow.ToString(),
            AutoDeleteOnIdle = topic.AutoDeleteOnIdle.ToString(),
            topic.SupportOrdering
        }.MatchInlineSnapshot(
            """
            {
              "AutoProvision": true,
              "DefaultMessageTimeToLive": "02:00:00",
              "MaxSizeInMegabytes": 4096,
              "EnablePartitioning": true,
              "RequiresDuplicateDetection": true,
              "DuplicateDetectionHistoryTimeWindow": "00:10:00",
              "AutoDeleteOnIdle": "01:00:00",
              "SupportOrdering": false
            }
            """);
    }

    [Fact]
    public void SubscriptionDescriptor_Should_MaterializeOptions_When_Configured()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareTopic("orders");
            t.DeclareQueue("accounting");
            t.DeclareQueue("archive");
            t.DeclareSubscription("orders", "accounting")
                .AutoProvision(true)
                .LockDuration(TimeSpan.FromSeconds(30))
                .MaxDeliveryCount(5)
                .DefaultMessageTimeToLive(TimeSpan.FromHours(1))
                .ForwardTo("archive")
                .ForwardDeadLetteredMessagesTo("archive")
                .DeadLetteringOnMessageExpiration(true)
                .AutoDeleteOnIdle(TimeSpan.FromHours(2));
        });
        var subscription = topology.Subscriptions.Single();

        // assert
        new
        {
            subscription.AutoProvision,
            LockDuration = subscription.LockDuration.ToString(),
            subscription.MaxDeliveryCount,
            DefaultMessageTimeToLive = subscription.DefaultMessageTimeToLive.ToString(),
            subscription.ForwardTo,
            subscription.ForwardDeadLetteredMessagesTo,
            subscription.DeadLetteringOnMessageExpiration,
            AutoDeleteOnIdle = subscription.AutoDeleteOnIdle.ToString()
        }.MatchInlineSnapshot(
            """
            {
              "AutoProvision": true,
              "LockDuration": "00:00:30",
              "MaxDeliveryCount": 5,
              "DefaultMessageTimeToLive": "01:00:00",
              "ForwardTo": "archive",
              "ForwardDeadLetteredMessagesTo": "archive",
              "DeadLetteringOnMessageExpiration": true,
              "AutoDeleteOnIdle": "02:00:00"
            }
            """);
    }

    private static (
        MessagingRuntime Runtime,
        AzureServiceBusMessagingTransport Transport,
        AzureServiceBusMessagingTopology Topology) CreateTopology(
        Action<IAzureServiceBusMessagingTransportDescriptor> configure)
    {
        var runtime = CreateRuntime(configure);
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        return (runtime, transport, (AzureServiceBusMessagingTopology)transport.Topology);
    }

    private static MessagingRuntime CreateRuntime(Action<IAzureServiceBusMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        return services
            .AddMessageBus()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(DummyConnectionString);
                configure(t);
            })
            .BuildRuntime();
    }

    public sealed class OrderMessage;
}
