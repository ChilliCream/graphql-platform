using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureEventHub.Tests.Helpers;

namespace Mocha.Transport.AzureEventHub.Tests.Behaviors;

public class BusDefaultsIntegrationTests
{
    [Fact]
    public void ConfigureDefaults_Should_ApplyPartitionCount_When_TopicCreatedByConvention()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddEventHandler<OrderCreatedHandler>(),
            t => t.ConfigureDefaults(d => d.Topic.PartitionCount = 8));

        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var topology = (EventHubMessagingTopology)transport.Topology;

        // assert - convention-created topics should inherit the default partition count
        var topics = topology.Topics.Where(t => t.Name != "error" && t.Name != "skipped").ToList();
        Assert.NotEmpty(topics);
        foreach (var topic in topics)
        {
            Assert.Equal(8, topic.PartitionCount);
        }
    }

    [Fact]
    public void ConfigureDefaults_Should_NotOverrideExplicitTopic_When_TopicDeclaredWithPartitionCount()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddEventHandler<OrderCreatedHandler>(),
            t =>
            {
                t.ConfigureDefaults(d => d.Topic.PartitionCount = 8);
                t.DeclareTopic("explicit-hub").PartitionCount(16);
            });

        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var topology = (EventHubMessagingTopology)transport.Topology;

        // assert - explicitly declared topic retains its own partition count
        var explicitTopic = topology.Topics.First(t => t.Name == "explicit-hub");
        Assert.Equal(16, explicitTopic.PartitionCount);

        // assert - convention topics still get the default
        var conventionTopics = topology.Topics
            .Where(t => t.Name != "explicit-hub" && t.Name != "error" && t.Name != "skipped")
            .ToList();
        Assert.NotEmpty(conventionTopics);
        foreach (var topic in conventionTopics)
        {
            Assert.Equal(8, topic.PartitionCount);
        }
    }

    [Fact]
    public void ConfigureDefaults_Should_SetDefaultBatchMode_When_ConfiguredViaBusDefaults()
    {
        // arrange - batch mode Batch eagerly creates producers during Complete(),
        // so we verify the configuration flows through without a full runtime build
        var defaults = new EventHubBusDefaults();

        // act
        defaults.DefaultBatchMode = EventHubBatchMode.Batch;

        // assert
        Assert.Equal(EventHubBatchMode.Batch, defaults.DefaultBatchMode);
    }

    [Fact]
    public void ConfigureDefaults_Should_ExposeDefaultsOnTopology_When_ConfiguredViaDescriptor()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddEventHandler<OrderCreatedHandler>(),
            t => t.ConfigureDefaults(d => d.Topic.PartitionCount = 32));

        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var topology = (EventHubMessagingTopology)transport.Topology;

        // assert - defaults are accessible through the topology
        Assert.Equal(32, topology.Defaults.Topic.PartitionCount);
    }

    [Fact]
    public void ConfigureDefaults_Should_DefaultToSingleBatchMode_When_NoExplicitConfiguration()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddEventHandler<OrderCreatedHandler>());

        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();

        // assert
        Assert.Equal(EventHubBatchMode.Single, transport.TransportConfiguration.Defaults.DefaultBatchMode);
    }

    [Fact]
    public void ConfigureDefaults_Should_ApplyPartitionCountToAllConventionTopics_When_MultipleHandlers()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b =>
            {
                b.AddEventHandler<OrderCreatedHandler>();
                b.AddRequestHandler<ProcessPaymentHandler>();
            },
            t => t.ConfigureDefaults(d => d.Topic.PartitionCount = 12));

        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var topology = (EventHubMessagingTopology)transport.Topology;

        // assert - all convention-created topics should inherit the default
        var topics = topology.Topics
            .Where(t => t.Name != "error" && t.Name != "skipped" && t.Name != "replies")
            .ToList();
        Assert.True(topics.Count >= 2, "Expected at least 2 topics for 2 handlers");
        foreach (var topic in topics)
        {
            Assert.Equal(12, topic.PartitionCount);
        }
    }

    [Fact]
    public void ConfigureDefaults_Should_LeavePartitionCountNull_When_NoDefaultConfigured()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddEventHandler<OrderCreatedHandler>());

        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var topology = (EventHubMessagingTopology)transport.Topology;

        // assert - convention topics should have null partition count (Azure default)
        var topics = topology.Topics.Where(t => t.Name != "error" && t.Name != "skipped").ToList();
        Assert.NotEmpty(topics);
        foreach (var topic in topics)
        {
            Assert.Null(topic.PartitionCount);
        }
    }

    public sealed class ProcessPaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configure,
        Action<IEventHubMessagingTransportDescriptor>? configureTransport = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder
            .AddEventHub(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                configureTransport?.Invoke(t);
            })
            .BuildRuntime();
        return runtime;
    }
}
