using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests;

public class InMemoryTransportTests
{
    [Fact]
    public async Task TryGetDispatchEndpoint_Should_ResolveToQueue_When_QueueSchemeUsed()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // The ProcessPayment handler creates a queue named "process-payment"
        var queueUri = new Uri("queue://process-payment");

        // act
        var found = transport.TryGetDispatchEndpoint(queueUri, out var endpoint);

        // assert
        Assert.True(found, "TryGetDispatchEndpoint should resolve queue:// URI");
        Assert.NotNull(endpoint);
        Assert.IsType<InMemoryQueue>(endpoint!.Destination);
        var queue = (InMemoryQueue)endpoint.Destination;
        Assert.Equal("process-payment", queue.Name);
    }

    [Fact]
    public async Task TryGetDispatchEndpoint_Should_ResolveToTopic_When_TopicSchemeUsed()
    {
        // arrange - start the bus with an event handler so a topic dispatch endpoint exists
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        // Find a topic dispatch endpoint - the publish endpoint for OrderCreated
        // The publish topic follows convention: namespace.order-created
        var topicEndpoint = transport.DispatchEndpoints.FirstOrDefault(e => e.Destination is InMemoryTopic);

        Assert.NotNull(topicEndpoint);
        var topicName = ((InMemoryTopic)topicEndpoint!.Destination).Name;

        var topicUri = new Uri($"topic://{topicName}");

        // act
        var found = transport.TryGetDispatchEndpoint(topicUri, out var endpoint);

        // assert
        Assert.True(found, "TryGetDispatchEndpoint should resolve topic:// URI");
        Assert.NotNull(endpoint);
        Assert.IsType<InMemoryTopic>(endpoint!.Destination);
        Assert.Equal(topicName, ((InMemoryTopic)endpoint.Destination).Name);
    }

    [Fact]
    public async Task TryGetDispatchEndpoint_Should_Resolve_When_MemorySchemeMatchesAddress()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // Get an actual dispatch endpoint to use its address
        var existingEndpoint = transport.DispatchEndpoints.First();
        var address = existingEndpoint.Address;

        // act
        var found = transport.TryGetDispatchEndpoint(address, out var endpoint);

        // assert
        Assert.True(found, "TryGetDispatchEndpoint should resolve matching address");
        Assert.NotNull(endpoint);
        Assert.Same(existingEndpoint, endpoint);
    }

    [Fact]
    public async Task TryGetDispatchEndpoint_Should_Resolve_When_TopologyBaseAddressUsed()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        // Find a dispatch endpoint that has a destination with an address that is based on the topology address
        var dispatchEndpoint = transport.DispatchEndpoints.FirstOrDefault(e =>
            e.Destination?.Address != null && topology.Address.IsBaseOf(e.Destination.Address)
        );

        Assert.NotNull(dispatchEndpoint);

        var destinationAddress = dispatchEndpoint!.Destination.Address;

        // act
        var found = transport.TryGetDispatchEndpoint(destinationAddress, out var endpoint);

        // assert
        Assert.True(found, "TryGetDispatchEndpoint should resolve destination address under topology base");
        Assert.NotNull(endpoint);
        Assert.Same(dispatchEndpoint, endpoint);
    }

    [Fact]
    public async Task TryGetDispatchEndpoint_Should_ReturnFalse_When_UnknownUri()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        var unknownUri = new Uri("http://unknown-host/nonexistent");

        // act
        var found = transport.TryGetDispatchEndpoint(unknownUri, out var endpoint);

        // assert
        Assert.False(found, "TryGetDispatchEndpoint should return false for unknown URI");
        Assert.Null(endpoint);
    }

    [Fact]
    public async Task TryGetDispatchEndpoint_Should_ReturnFalse_When_QueueNotFound()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        var nonexistentUri = new Uri("queue://nonexistent-queue");

        // act
        var found = transport.TryGetDispatchEndpoint(nonexistentUri, out var endpoint);

        // assert
        Assert.False(found, "TryGetDispatchEndpoint should return false for nonexistent queue");
        Assert.Null(endpoint);
    }

    [Fact]
    public async Task TryGetDispatchEndpoint_Should_ReturnFalse_When_TopicNotFound()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        var nonexistentUri = new Uri("topic://nonexistent-topic");

        // act
        var found = transport.TryGetDispatchEndpoint(nonexistentUri, out var endpoint);

        // assert
        Assert.False(found, "TryGetDispatchEndpoint should return false for nonexistent topic");
        Assert.Null(endpoint);
    }

    [Fact]
    public async Task TryGetDispatchEndpoint_Should_ReturnFalse_When_MemorySchemeNoMatch()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        var memoryUri = new Uri("memory://no-match/some-endpoint");

        // act
        var found = transport.TryGetDispatchEndpoint(memoryUri, out var endpoint);

        // assert
        Assert.False(found, "TryGetDispatchEndpoint should return false for non-matching memory:// URI");
        Assert.Null(endpoint);
    }

    [Fact]
    public async Task Describe_Should_ReturnDescription_When_EventHandlerRegistered()
    {
        // arrange - start with an event handler to create topics, queues, and bindings
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        Assert.NotNull(description);
        Assert.Equal("memory", description.Schema);
        Assert.Equal("InMemoryMessagingTransport", description.TransportType);
        Assert.NotNull(description.Topology);
    }

    [Fact]
    public async Task Describe_Should_IncludeTopicEntities_When_EventHandlerRegistered()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert - topology should contain topic entities
        Assert.NotNull(description.Topology);
        Assert.Contains(description.Topology!.Entities, e => e.Kind == "topic");
    }

    [Fact]
    public async Task Describe_Should_IncludeQueueEntities_When_EventHandlerRegistered()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert - topology should contain queue entities
        Assert.NotNull(description.Topology);
        Assert.Contains(description.Topology!.Entities, e => e.Kind == "queue");
    }

    [Fact]
    public async Task Describe_Should_IncludeBindingLinks_When_EventHandlerRegistered()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert - topology should contain links for bindings
        Assert.NotNull(description.Topology);
        Assert.NotEmpty(description.Topology!.Links);
        Assert.All(
            description.Topology.Links,
            link =>
            {
                Assert.Equal("bind", link.Kind);
                Assert.Equal("forward", link.Direction);
            });
    }

    [Fact]
    public async Task Describe_Should_IncludeReceiveEndpoints_When_EventHandlerRegistered()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert - receive endpoints should have name and kind populated
        Assert.NotEmpty(description.ReceiveEndpoints);
        Assert.All(
            description.ReceiveEndpoints,
            ep =>
            {
                Assert.NotNull(ep.Name);
                Assert.NotEmpty(ep.Name);
                Assert.True(
                    ep.Kind is ReceiveEndpointKind.Default or ReceiveEndpointKind.Reply,
                    $"Expected Default or Reply kind, got {ep.Kind}");
            });
    }

    [Fact]
    public async Task Describe_Should_IncludeDispatchEndpoints_When_EventHandlerRegistered()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert - dispatch endpoints should have name, kind, and address populated
        Assert.NotEmpty(description.DispatchEndpoints);
        Assert.All(
            description.DispatchEndpoints,
            ep =>
            {
                Assert.NotNull(ep.Name);
                Assert.NotEmpty(ep.Name);
                Assert.True(
                    ep.Kind is DispatchEndpointKind.Default or DispatchEndpointKind.Reply,
                    $"Expected Default or Reply kind, got {ep.Kind}");
                Assert.NotNull(ep.Address);
            });
    }

    [Fact]
    public async Task Describe_Should_IncludeTopicBindingLink_When_TopicToTopicBinding()
    {
        // arrange - create runtime then manually add topic-to-topic binding
        var (runtime, transport, topology) = CreateTopology(b =>
            b.AddEventHandler<OrderCreatedHandler>());

        // Add custom topic-to-topic binding
        topology.AddTopic(new InMemoryTopicConfiguration { Name = "source-for-describe" });
        topology.AddTopic(new InMemoryTopicConfiguration { Name = "dest-for-describe" });
        topology.AddBinding(
            new InMemoryBindingConfiguration
            {
                Source = "source-for-describe",
                Destination = "dest-for-describe",
                DestinationKind = InMemoryDestinationKind.Topic
            });

        // Start the runtime so topology is fully configured
        await runtime.StartAsync(CancellationToken.None);

        // act
        var description = transport.Describe();

        // assert - there should be a link representing the topic-to-topic binding
        Assert.NotNull(description.Topology);
        Assert.NotEmpty(description.Topology!.Links);

        // The topic-to-topic binding link has the address path containing /b/t/source/t/dest
        var topicToTopicLink = Assert.Single(
            description.Topology.Links,
            link => link.Address?.Contains("/b/t/source-for-describe/t/dest-for-describe") == true);

        Assert.Equal("bind", topicToTopicLink.Kind);
        Assert.Equal("forward", topicToTopicLink.Direction);
    }

    [Fact]
    public void AddBinding_Should_Throw_When_DestinationTopicNotFound()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddTopic(new InMemoryTopicConfiguration { Name = "source-topic" });

        var bindingConfig = new InMemoryBindingConfiguration
        {
            Source = "source-topic",
            Destination = "nonexistent-topic",
            DestinationKind = InMemoryDestinationKind.Topic
        };

        // act & assert
        var exception = Assert.Throws<InvalidOperationException>(() => topology.AddBinding(bindingConfig));
        Assert.Contains("nonexistent-topic", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void AddBinding_Should_Throw_When_UnknownDestinationKind()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddTopic(new InMemoryTopicConfiguration { Name = "source-topic" });

        var bindingConfig = new InMemoryBindingConfiguration
        {
            Source = "source-topic",
            Destination = "some-dest",
            DestinationKind = (InMemoryDestinationKind)99 // Unknown kind
        };

        // act & assert
        var exception = Assert.Throws<InvalidOperationException>(() => topology.AddBinding(bindingConfig));
        Assert.Contains("Unknown destination kind", exception.Message);
    }

    [Fact]
    public async Task SendAsync_Should_DeliverToQueue_When_RequestHandlerRegistered()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - send to queue (point-to-point)
        await bus.SendAsync(new ProcessPayment { OrderId = "ORD-T1", Amount = 42.00m }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout), "Handler did not receive the message sent to queue");

        var msg = Assert.Single(recorder.Messages);
        var payment = Assert.IsType<ProcessPayment>(msg);
        Assert.Equal("ORD-T1", payment.OrderId);
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverToSubscriber_When_EventHandlerRegistered()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish to topic (pub/sub)
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-T2" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout), "Handler did not receive the message published to topic");

        var msg = Assert.Single(recorder.Messages);
        var order = Assert.IsType<OrderCreated>(msg);
        Assert.Equal("ORD-T2", order.OrderId);
    }

    [Fact]
    public async Task CreateEndpointConfiguration_Should_CreateQueueConfig_When_SendRoute()
    {
        // arrange - build a runtime so the transport is initialized
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // The send routes should have dispatch endpoints with queue names
        var queueEndpoint = transport.DispatchEndpoints.FirstOrDefault(e =>
            e.Destination is InMemoryQueue && e.Kind == DispatchEndpointKind.Default
        );

        Assert.NotNull(queueEndpoint);
        Assert.StartsWith("q/", queueEndpoint!.Name);
    }

    [Fact]
    public async Task CreateEndpointConfiguration_Should_CreateTopicConfig_When_PublishRoute()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // The publish routes should have dispatch endpoints with topic names
        var topicEndpoint = transport.DispatchEndpoints.FirstOrDefault(e => e.Destination is InMemoryTopic);

        Assert.NotNull(topicEndpoint);
        Assert.StartsWith("t/", topicEndpoint!.Name);
    }

    [Fact]
    public async Task RequestAsync_Should_UseReplyEndpoints_When_RoundTrip()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // Verify reply endpoints exist
        var replyReceive = transport.ReceiveEndpoints.FirstOrDefault(e => e.Kind == ReceiveEndpointKind.Reply);
        var replyDispatch = transport.DispatchEndpoints.FirstOrDefault(e => e.Kind == DispatchEndpointKind.Reply);

        Assert.NotNull(replyReceive);
        Assert.NotNull(replyDispatch);

        // act - perform request/response
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var response = await bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-RR1" }, CancellationToken.None);

        // assert
        Assert.NotNull(response);
        Assert.Equal("ORD-RR1", response.OrderId);
        Assert.Equal("Shipped", response.Status);
    }

    [Fact]
    public void GetTopic_Should_ReturnTopic_When_SpanMatchesName()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });
        var added = topology.AddTopic(new InMemoryTopicConfiguration { Name = "span-topic" });

        // act
        var found = topology.GetTopic("span-topic".AsSpan());

        // assert
        Assert.NotNull(found);
        Assert.Same(added, found);
    }

    [Fact]
    public void GetTopic_Should_ReturnNull_When_SpanNotFound()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        // act
        var found = topology.GetTopic("nonexistent".AsSpan());

        // assert
        Assert.Null(found);
    }

    [Fact]
    public void GetQueue_Should_ReturnQueue_When_SpanMatchesName()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });
        var added = topology.AddQueue(new InMemoryQueueConfiguration { Name = "span-queue" });

        // act
        var found = topology.GetQueue("span-queue".AsSpan());

        // assert
        Assert.NotNull(found);
        Assert.Same(added, found);
    }

    [Fact]
    public void GetQueue_Should_ReturnNull_When_SpanNotFound()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        // act
        var found = topology.GetQueue("nonexistent".AsSpan());

        // assert
        Assert.Null(found);
    }

    [Fact]
    public async Task Describe_Should_IncludeQueueEntity_When_RequestHandlerRegistered()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert - should have queue entity for process-payment
        Assert.NotNull(description.Topology);
        Assert.Contains(description.Topology!.Entities, e => e.Kind == "queue" && e.Name == "process-payment");
    }

    [Fact]
    public async Task Describe_Should_IncludeAllTopology_When_MultipleHandlersRegistered()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        Assert.NotNull(description.Topology);
        var topicCount = description.Topology!.Entities.Count(e => e.Kind == "topic");
        var queueCount = description.Topology.Entities.Count(e => e.Kind == "queue");

        // Both event handler (creates topic + queue) and request handler (creates queue) contribute
        Assert.True(topicCount >= 1, "Should have at least one topic entity");
        Assert.True(queueCount >= 2, "Should have at least two queue entities");
    }

    [Fact]
    public async Task Describe_Should_MatchTopologyAddress_When_EventHandlerRegistered()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        // act
        var description = transport.Describe();

        // assert
        Assert.NotNull(description.Topology);
        Assert.Equal(topology.Address.ToString(), description.Topology!.Address);
        Assert.Equal(topology.Address.ToString(), description.Identifier);
    }

    [Fact]
    public async Task DispatchEndpoints_Should_HaveTopicDestination_When_EventHandlerRegistered()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // act - find dispatch endpoint for publish
        var publishEndpoint = transport.DispatchEndpoints.Where(e => e.Destination is InMemoryTopic).ToList();

        // assert - event handlers create a publish (topic) dispatch endpoint
        Assert.NotEmpty(publishEndpoint);
        var topicDispatch = publishEndpoint.First();
        Assert.StartsWith("t/", topicDispatch.Name);
        Assert.Equal(DispatchEndpointKind.Default, topicDispatch.Kind);
        Assert.NotNull(topicDispatch.Address);
        Assert.Contains("/t/", topicDispatch.Address!.AbsolutePath);
    }

    [Fact]
    public async Task DispatchEndpoints_Should_HaveQueueDestination_When_RequestHandlerRegistered()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // act - find dispatch endpoint for send (queue)
        var sendEndpoints = transport
            .DispatchEndpoints.Where(e => e.Destination is InMemoryQueue && e.Kind != DispatchEndpointKind.Reply)
            .ToList();

        // assert - request handlers create a send (queue) dispatch endpoint
        Assert.NotEmpty(sendEndpoints);
        var queueDispatch = sendEndpoints.First();
        Assert.StartsWith("q/", queueDispatch.Name);
        Assert.Equal(DispatchEndpointKind.Default, queueDispatch.Kind);
        Assert.NotNull(queueDispatch.Address);
        Assert.Contains("/q/", queueDispatch.Address!.AbsolutePath);
    }

    [Fact]
    public async Task ReceiveEndpoints_Should_HaveQueues_When_HandlersRegistered()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // act & assert - each receive endpoint should be of type InMemoryReceiveEndpoint
        foreach (var endpoint in transport.ReceiveEndpoints)
        {
            Assert.IsType<InMemoryReceiveEndpoint>(endpoint);
            var inMemoryEndpoint = (InMemoryReceiveEndpoint)endpoint;
            Assert.NotNull(inMemoryEndpoint.Queue);
        }
    }

    [Fact]
    public async Task TryGetDispatchEndpoint_Should_ResolveMultipleQueues_When_DifferentHandlers()
    {
        // arrange - create multiple request handlers for different queues
        var recorder1 = new MessageRecorder();
        var recorder2 = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddKeyedSingleton("r1", recorder1)
            .AddKeyedSingleton("r2", recorder2)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentKeyedHandler1>()
            .AddRequestHandler<ProcessRefundKeyedHandler2>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // act - look up both queues
        var foundPayment = transport.TryGetDispatchEndpoint(
            new Uri("queue://process-payment"),
            out var paymentEndpoint);
        var foundRefund = transport.TryGetDispatchEndpoint(new Uri("queue://process-refund"), out var refundEndpoint);

        // assert
        Assert.True(foundPayment, "Should find process-payment queue");
        Assert.True(foundRefund, "Should find process-refund queue");
        Assert.NotSame(paymentEndpoint, refundEndpoint);
    }

    [Fact]
    public void QueueBinding_Should_HaveExpectedPathSegments_When_Created()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddTopic(new InMemoryTopicConfiguration { Name = "addr-source" });
        topology.AddQueue(new InMemoryQueueConfiguration { Name = "addr-dest" });
        var binding = topology.AddBinding(
            new InMemoryBindingConfiguration
            {
                Source = "addr-source",
                Destination = "addr-dest",
                DestinationKind = InMemoryDestinationKind.Queue
            });

        // act
        var queueBinding = Assert.IsType<InMemoryQueueBinding>(binding);

        // assert - binding address should contain path segments b/t/source/q/dest
        Assert.NotNull(queueBinding.Address);
        var path = queueBinding.Address!.AbsolutePath;
        Assert.Contains("/b/t/addr-source/q/addr-dest", path);
    }

    [Fact]
    public void TopicBinding_Should_HaveExpectedPathSegments_When_Created()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddTopic(new InMemoryTopicConfiguration { Name = "addr-source-t" });
        topology.AddTopic(new InMemoryTopicConfiguration { Name = "addr-dest-t" });
        var binding = topology.AddBinding(
            new InMemoryBindingConfiguration
            {
                Source = "addr-source-t",
                Destination = "addr-dest-t",
                DestinationKind = InMemoryDestinationKind.Topic
            });

        // act
        var topicBinding = Assert.IsType<InMemoryTopicBinding>(binding);

        // assert - binding address should contain path segments b/t/source/t/dest
        Assert.NotNull(topicBinding.Address);
        var path = topicBinding.Address!.AbsolutePath;
        Assert.Contains("/b/t/addr-source-t/t/addr-dest-t", path);
    }

    [Fact]
    public void Schema_Should_BeMemory_When_TransportCreated()
    {
        // arrange & act
        var (_, transport, _) = CreateTopology(_ => { });

        // assert
        Assert.Equal("memory", transport.Schema);
    }

    [Fact]
    public void TopologyAddress_Should_UseMemoryScheme_When_TransportCreated()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(_ => { });

        // assert
        Assert.Equal("memory", topology.Address.Scheme);
    }

    [Fact]
    public void Topology_Should_BeInMemoryMessagingTopology_When_TransportCreated()
    {
        // arrange & act
        var (_, transport, _) = CreateTopology(_ => { });

        // assert
        Assert.IsType<InMemoryMessagingTopology>(transport.Topology);
    }

    [Fact]
    public async Task ReceiveEndpoint_Should_BeStarted_When_RuntimeStarted()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // act & assert - after CreateBusAsync, runtime is started and endpoints should be started
        foreach (var endpoint in transport.ReceiveEndpoints)
        {
            var inMemoryEndpoint = Assert.IsType<InMemoryReceiveEndpoint>(endpoint);
            Assert.True(inMemoryEndpoint.IsStarted, $"Receive endpoint '{endpoint.Name}' should be started");
        }
    }

    [Fact]
    public void Binding_Should_HaveCorrectSource_When_Created()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddTopic(new InMemoryTopicConfiguration { Name = "src-topic" });
        topology.AddQueue(new InMemoryQueueConfiguration { Name = "dst-queue" });

        // act
        var binding = topology.AddBinding(
            new InMemoryBindingConfiguration
            {
                Source = "src-topic",
                Destination = "dst-queue",
                DestinationKind = InMemoryDestinationKind.Queue
            });

        // assert
        Assert.NotNull(binding.Source);
        Assert.Equal("src-topic", binding.Source.Name);
    }

    [Fact]
    public void TopicBinding_Should_HaveCorrectDestination_When_Created()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddTopic(new InMemoryTopicConfiguration { Name = "src-t" });
        topology.AddTopic(new InMemoryTopicConfiguration { Name = "dst-t" });

        // act
        var binding = topology.AddBinding(
            new InMemoryBindingConfiguration
            {
                Source = "src-t",
                Destination = "dst-t",
                DestinationKind = InMemoryDestinationKind.Topic
            });

        // assert
        var topicBinding = Assert.IsType<InMemoryTopicBinding>(binding);
        Assert.NotNull(topicBinding.Destination);
        Assert.Equal("dst-t", topicBinding.Destination.Name);
    }

    public sealed class ProcessRefund
    {
        public required string OrderId { get; init; }
        public required decimal Amount { get; init; }
    }

    public sealed class ProcessPaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }

    public sealed class ProcessPaymentKeyedHandler1([FromKeyedServices("r1")] MessageRecorder recorder)
        : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }

    public sealed class ProcessRefundKeyedHandler2([FromKeyedServices("r2")] MessageRecorder recorder)
        : IEventRequestHandler<ProcessRefund>
    {
        public ValueTask HandleAsync(ProcessRefund request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }

    public sealed class GetOrderStatusHandler(MessageRecorder recorder)
        : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
    {
        public ValueTask<OrderStatusResponse> HandleAsync(GetOrderStatus request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return new(new OrderStatusResponse { OrderId = request.OrderId, Status = "Shipped" });
        }
    }

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static (
        MessagingRuntime Runtime,
        InMemoryMessagingTransport Transport,
        InMemoryMessagingTopology Topology) CreateTopology(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder.AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;
        return (runtime, transport, topology);
    }
}
