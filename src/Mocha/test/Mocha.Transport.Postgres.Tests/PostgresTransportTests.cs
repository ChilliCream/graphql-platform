using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests;

public class PostgresTransportTests
{
    [Fact]
    public void Schema_Should_BePostgres_When_TransportCreated()
    {
        // arrange & act
        var (_, transport, _) = PostgresBusFixture.CreateTopology(_ => { });

        // assert
        Assert.Equal("postgres", transport.Schema);
    }

    [Fact]
    public void TopologyAddress_Should_UsePostgresScheme_When_TransportCreated()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // assert
        Assert.Equal("postgres", topology.Address.Scheme);
    }

    [Fact]
    public void Topology_Should_BePostgresMessagingTopology_When_TransportCreated()
    {
        // arrange & act
        var (_, transport, _) = PostgresBusFixture.CreateTopology(_ => { });

        // assert
        Assert.IsType<PostgresMessagingTopology>(transport.Topology);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_ResolveToQueue_When_QueueSchemeUsed()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("payment");
            t.DispatchEndpoint("ep").ToQueue("payment").Send<ProcessPayment>();
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        var queueUri = new Uri("queue://payment");

        // act
        var found = transport.TryGetDispatchEndpoint(queueUri, out var endpoint);

        // assert
        Assert.True(found, "TryGetDispatchEndpoint should resolve queue:// URI");
        Assert.NotNull(endpoint);
        Assert.IsType<PostgresQueue>(endpoint!.Destination);
        Assert.Equal("payment", ((PostgresQueue)endpoint.Destination).Name);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_ResolveToTopic_When_TopicSchemeUsed()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("events");
            t.DispatchEndpoint("ep").ToTopic("events").Publish<OrderCreated>();
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        var topicUri = new Uri("topic://events");

        // act
        var found = transport.TryGetDispatchEndpoint(topicUri, out var endpoint);

        // assert
        Assert.True(found, "TryGetDispatchEndpoint should resolve topic:// URI");
        Assert.NotNull(endpoint);
        Assert.IsType<PostgresTopic>(endpoint!.Destination);
        Assert.Equal("events", ((PostgresTopic)endpoint.Destination).Name);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_Resolve_When_PostgresSchemeMatchesAddress()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("my-q");
            t.DispatchEndpoint("ep").ToQueue("my-q");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        var existingEndpoint = transport.DispatchEndpoints.First();
        var address = existingEndpoint.Address;

        // act
        var found = transport.TryGetDispatchEndpoint(address, out var endpoint);

        // assert
        Assert.True(found);
        Assert.NotNull(endpoint);
        Assert.Same(existingEndpoint, endpoint);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_Resolve_When_TopologyBaseAddressUsed()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("base-q");
            t.DispatchEndpoint("ep").ToQueue("base-q");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var topology = (PostgresMessagingTopology)transport.Topology;

        var dispatchEndpoint = transport.DispatchEndpoints.FirstOrDefault(e =>
            e.Destination?.Address != null && topology.Address.IsBaseOf(e.Destination.Address));

        Assert.NotNull(dispatchEndpoint);
        var destinationAddress = dispatchEndpoint!.Destination.Address;

        // act
        var found = transport.TryGetDispatchEndpoint(destinationAddress, out var endpoint);

        // assert
        Assert.True(found);
        Assert.NotNull(endpoint);
        Assert.Same(dispatchEndpoint, endpoint);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_ReturnFalse_When_UnknownUri()
    {
        // arrange
        var (_, transport, _) = PostgresBusFixture.CreateTopology(_ => { });
        var unknownUri = new Uri("http://unknown-host/nonexistent");

        // act
        var found = transport.TryGetDispatchEndpoint(unknownUri, out var endpoint);

        // assert
        Assert.False(found);
        Assert.Null(endpoint);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_ReturnFalse_When_QueueNotFound()
    {
        // arrange
        var (_, transport, _) = PostgresBusFixture.CreateTopology(_ => { });
        var nonexistentUri = new Uri("queue://nonexistent-queue");

        // act
        var found = transport.TryGetDispatchEndpoint(nonexistentUri, out var endpoint);

        // assert
        Assert.False(found);
        Assert.Null(endpoint);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_ReturnFalse_When_TopicNotFound()
    {
        // arrange
        var (_, transport, _) = PostgresBusFixture.CreateTopology(_ => { });
        var nonexistentUri = new Uri("topic://nonexistent-topic");

        // act
        var found = transport.TryGetDispatchEndpoint(nonexistentUri, out var endpoint);

        // assert
        Assert.False(found);
        Assert.Null(endpoint);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_ReturnFalse_When_PostgresSchemeNoMatch()
    {
        // arrange
        var (_, transport, _) = PostgresBusFixture.CreateTopology(_ => { });
        var postgresUri = new Uri("postgres://no-match/some-endpoint");

        // act
        var found = transport.TryGetDispatchEndpoint(postgresUri, out var endpoint);

        // assert
        Assert.False(found);
        Assert.Null(endpoint);
    }

    [Fact]
    public void Describe_Should_ReturnDescription_When_TransportCreated()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("events");
            t.DeclareQueue("q1");
            t.DeclareSubscription("events", "q1");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        Assert.NotNull(description);
        Assert.Equal("postgres", description.Schema);
        Assert.Equal("PostgresMessagingTransport", description.TransportType);
        Assert.NotNull(description.Topology);
    }

    [Fact]
    public void Describe_Should_IncludeTopicEntities_When_TopicDeclared()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t => t.DeclareTopic("my-events"));
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        Assert.NotNull(description.Topology);
        Assert.Contains(description.Topology!.Entities, e => e.Kind == "topic" && e.Name == "my-events");
    }

    [Fact]
    public void Describe_Should_IncludeQueueEntities_When_QueueDeclared()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t => t.DeclareQueue("my-queue"));
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        Assert.NotNull(description.Topology);
        Assert.Contains(description.Topology!.Entities, e => e.Kind == "queue" && e.Name == "my-queue");
    }

    [Fact]
    public void Describe_Should_IncludeSubscriptionLinks_When_SubscriptionDeclared()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("src");
            t.DeclareQueue("dst");
            t.DeclareSubscription("src", "dst");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        Assert.NotNull(description.Topology);
        Assert.NotEmpty(description.Topology!.Links);
        var link = Assert.Single(description.Topology.Links);
        Assert.Equal("subscription", link.Kind);
        Assert.Equal("forward", link.Direction);
    }

    [Fact]
    public void Describe_Should_MatchTopologyAddress_When_Created()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t => t.DeclareTopic("events"));
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var topology = (PostgresMessagingTopology)transport.Topology;

        // act
        var description = transport.Describe();

        // assert
        Assert.NotNull(description.Topology);
        Assert.Equal(topology.Address.ToString(), description.Topology!.Address);
        Assert.Equal(topology.Address.ToString(), description.Identifier);
    }

    [Fact]
    public void Describe_Should_IncludeAllTopology_When_MultipleResourcesDeclared()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("t1");
            t.DeclareTopic("t2");
            t.DeclareQueue("q1");
            t.DeclareQueue("q2");
            t.DeclareSubscription("t1", "q1");
            t.DeclareSubscription("t2", "q2");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        Assert.NotNull(description.Topology);
        Assert.True(description.Topology!.Entities.Count(e => e.Kind == "topic") >= 2);
        Assert.True(description.Topology.Entities.Count(e => e.Kind == "queue") >= 2);
        Assert.True(description.Topology.Links.Count >= 2);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateQueueConfig_When_SendRoute()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("process-payment");
            t.DispatchEndpoint("ep").ToQueue("process-payment").Send<ProcessPayment>();
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var queueEndpoint = transport.DispatchEndpoints
            .FirstOrDefault(e => e.Destination is PostgresQueue && e.Kind == DispatchEndpointKind.Default);

        Assert.NotNull(queueEndpoint);
        Assert.IsType<PostgresQueue>(queueEndpoint!.Destination);
        Assert.Equal("process-payment", ((PostgresQueue)queueEndpoint.Destination).Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateTopicConfig_When_PublishRoute()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("order-created");
            t.DispatchEndpoint("ep").ToTopic("order-created").Publish<OrderCreated>();
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var topicEndpoint = transport.DispatchEndpoints
            .FirstOrDefault(e => e.Destination is PostgresTopic);

        Assert.NotNull(topicEndpoint);
        Assert.IsType<PostgresTopic>(topicEndpoint!.Destination);
        Assert.Equal("order-created", ((PostgresTopic)topicEndpoint.Destination).Name);
    }

    [Fact]
    public void DispatchEndpoints_Should_HaveQueueDestination_When_SendConfigured()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("payment-q");
            t.DispatchEndpoint("payment").ToQueue("payment-q").Send<ProcessPayment>();
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var queueEndpoints = transport.DispatchEndpoints
            .Where(e => e.Destination is PostgresQueue && e.Kind != DispatchEndpointKind.Reply)
            .ToList();

        // assert
        Assert.NotEmpty(queueEndpoints);
        var queueDispatch = queueEndpoints.First();
        Assert.NotNull(queueDispatch.Destination?.Address);
        Assert.Contains("/q/", queueDispatch.Destination!.Address!.AbsolutePath);
    }

    [Fact]
    public void DispatchEndpoints_Should_HaveTopicDestination_When_PublishConfigured()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("events-t");
            t.DispatchEndpoint("events").ToTopic("events-t").Publish<OrderCreated>();
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var topicEndpoints = transport.DispatchEndpoints
            .Where(e => e.Destination is PostgresTopic)
            .ToList();

        // assert
        Assert.NotEmpty(topicEndpoints);
        var topicDispatch = topicEndpoints.First();
        Assert.NotNull(topicDispatch.Destination?.Address);
        Assert.Contains("/t/", topicDispatch.Destination!.Address!.AbsolutePath);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_ResolveMultipleQueues_When_DifferentEndpoints()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("q1");
            t.DeclareQueue("q2");
            t.DispatchEndpoint("ep1").ToQueue("q1").Send<ProcessPayment>();
            t.DispatchEndpoint("ep2").ToQueue("q2").Send<OrderCreated>();
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var found1 = transport.TryGetDispatchEndpoint(new Uri("queue://q1"), out var endpoint1);
        var found2 = transport.TryGetDispatchEndpoint(new Uri("queue://q2"), out var endpoint2);

        // assert
        Assert.True(found1);
        Assert.True(found2);
        Assert.NotSame(endpoint1, endpoint2);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateQueueConfig_When_QueueSchemeUri()
    {
        // arrange
        var runtime = CreateRuntimeWithHandlers(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("queue:my-queue"));

        // assert
        Assert.NotNull(config);
        var pgConfig = Assert.IsType<PostgresDispatchEndpointConfiguration>(config);
        Assert.Equal("my-queue", pgConfig.QueueName);
        Assert.Equal("q/my-queue", pgConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateTopicConfig_When_TopicSchemeUri()
    {
        // arrange
        var runtime = CreateRuntimeWithHandlers(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("topic:my-topic"));

        // assert
        Assert.NotNull(config);
        var pgConfig = Assert.IsType<PostgresDispatchEndpointConfiguration>(config);
        Assert.Equal("my-topic", pgConfig.TopicName);
        Assert.Equal("t/my-topic", pgConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateQueueConfig_When_PostgresSchemeQueuePath()
    {
        // arrange
        var runtime = CreateRuntimeWithHandlers(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("postgres:///q/my-queue"));

        // assert
        Assert.NotNull(config);
        var pgConfig = Assert.IsType<PostgresDispatchEndpointConfiguration>(config);
        Assert.Equal("my-queue", pgConfig.QueueName);
        Assert.Equal("q/my-queue", pgConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateTopicConfig_When_PostgresSchemeTopicPath()
    {
        // arrange
        var runtime = CreateRuntimeWithHandlers(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("postgres:///t/my-topic"));

        // assert
        Assert.NotNull(config);
        var pgConfig = Assert.IsType<PostgresDispatchEndpointConfiguration>(config);
        Assert.Equal("my-topic", pgConfig.TopicName);
        Assert.Equal("t/my-topic", pgConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateReplyConfig_When_PostgresRepliesPath()
    {
        // arrange
        var runtime = CreateRuntimeWithHandlers(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("postgres:///replies"));

        // assert
        Assert.NotNull(config);
        var pgConfig = Assert.IsType<PostgresDispatchEndpointConfiguration>(config);
        Assert.Equal(DispatchEndpointKind.Reply, pgConfig.Kind);
        Assert.Equal("Replies", pgConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateQueueConfig_When_TopologyBaseQueueUri()
    {
        // arrange
        var runtime = CreateRuntimeWithHandlers(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;
        var topologyAddress = ((PostgresMessagingTopology)transport.Topology).Address;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri(topologyAddress, "q/my-queue"));

        // assert
        Assert.NotNull(config);
        var pgConfig = Assert.IsType<PostgresDispatchEndpointConfiguration>(config);
        Assert.Equal("my-queue", pgConfig.QueueName);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateTopicConfig_When_TopologyBaseTopicUri()
    {
        // arrange
        var runtime = CreateRuntimeWithHandlers(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;
        var topologyAddress = ((PostgresMessagingTopology)transport.Topology).Address;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri(topologyAddress, "t/my-topic"));

        // assert
        Assert.NotNull(config);
        var pgConfig = Assert.IsType<PostgresDispatchEndpointConfiguration>(config);
        Assert.Equal("my-topic", pgConfig.TopicName);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_ReturnNull_When_UnknownScheme()
    {
        // arrange
        var runtime = CreateRuntimeWithHandlers(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("http://foo/bar"));

        // assert
        Assert.Null(config);
    }

    [Fact]
    public void Convention_Should_SetErrorAndSkippedEndpoints_When_DefaultEndpoint()
    {
        // arrange
        var runtime = CreateRuntimeWithHandlers(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var receiveEndpoint = transport.ReceiveEndpoints.First(e => e.Kind == ReceiveEndpointKind.Default);

        // assert
        Assert.NotNull(receiveEndpoint.ErrorEndpoint);
        Assert.Contains("_error", receiveEndpoint.ErrorEndpoint!.Name);

        Assert.NotNull(receiveEndpoint.SkippedEndpoint);
        Assert.Contains("_skipped", receiveEndpoint.SkippedEndpoint!.Name);
    }

    [Fact]
    public async Task DisposeAsync_Should_NotThrow_When_Called()
    {
        // arrange
        var runtime = CreateRuntimeWithHandlers(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act & assert - no exception thrown
        await transport.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_Should_BeIdempotent_When_CalledTwice()
    {
        // arrange
        var runtime = CreateRuntimeWithHandlers(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act - call twice
        await transport.DisposeAsync();
        await transport.DisposeAsync();

        // assert - no exception
    }

    private static MessagingRuntime CreateRuntimeWithHandlers(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder
            .AddPostgres(t => t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test"))
            .BuildRuntime();
        return runtime;
    }
}
