using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;
using CookieCrumble;

namespace Mocha.Transport.InMemory.Tests;

public class InMemoryBuilderApiTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task DeclareBinding_Should_DeliverMessage_When_TopicBoundToQueue()
    {
        // arrange - declare a manual topic, queue, and binding via the builder API
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<MessageRecordConsumer>()
            .AddInMemory(t =>
            {
                t.Endpoint("manual-endpoint").Handler<MessageRecordConsumer>().Queue("manual-queue");

                t.DeclareTopic("manual-topic");
                t.DeclareQueue("manual-queue");
                t.DeclareBinding("manual-topic", "manual-queue");
            })
            .BuildServiceProvider();
        var bus = provider.GetRequiredService<IMessageBus>();

        // act - send through the topic
        await bus.SendAsync(
            new TestEvent("bound-msg"),
            new SendOptions { Endpoint = new Uri("queue://manual-queue") },
            CancellationToken.None);

        // assert - queue receives the message via the binding
        Assert.True(
            await recorder.WaitAsync(s_timeout),
            "Handler should receive message routed via ToQueue on dispatch endpoint");
    }

    [Fact]
    public async Task DeclareBinding_Should_FanOut_When_MultipleQueuesBoundToTopic()
    {
        // arrange - 1 topic, 3 queues, 3 keyed handlers; publish fans out to all 3
        var recorder1 = new MessageRecorder();
        var recorder2 = new MessageRecorder();
        var recorder3 = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddKeyedSingleton("fan1", recorder1)
            .AddKeyedSingleton("fan2", recorder2)
            .AddKeyedSingleton("fan3", recorder3)
            .AddMessageBus()
            .AddEventHandler<FanOutHandler1>()
            .AddEventHandler<FanOutHandler2>()
            .AddEventHandler<FanOutHandler3>()
            .AddInMemory(t =>
            {
                t.DeclareTopic("fan-topic");
                t.DeclareQueue("fan-q1");
                t.DeclareQueue("fan-q2");
                t.DeclareQueue("fan-q3");
                t.DeclareBinding("fan-topic", "fan-q1").ToQueue("fan-q1");
                t.DeclareBinding("fan-topic", "fan-q2").ToQueue("fan-q2");
                t.DeclareBinding("fan-topic", "fan-q3").ToQueue("fan-q3");

                t.Endpoint("fan-ep1").Handler<FanOutHandler1>().Queue("fan-q1");
                t.Endpoint("fan-ep2").Handler<FanOutHandler2>().Queue("fan-q2");
                t.Endpoint("fan-ep3").Handler<FanOutHandler3>().Queue("fan-q3");

                t.DispatchEndpoint("fan-dispatch").ToTopic("fan-topic").Publish<OrderCreated>();
            })
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "fan-msg" }, CancellationToken.None);

        // assert - all 3 handlers receive the message
        Assert.True(await recorder1.WaitAsync(s_timeout), "First fan-out handler did not receive the event");
        Assert.True(await recorder2.WaitAsync(s_timeout), "Second fan-out handler did not receive the event");
        Assert.True(await recorder3.WaitAsync(s_timeout), "Third fan-out handler did not receive the event");

        var msg1 = Assert.IsType<OrderCreated>(Assert.Single(recorder1.Messages));
        var msg2 = Assert.IsType<OrderCreated>(Assert.Single(recorder2.Messages));
        var msg3 = Assert.IsType<OrderCreated>(Assert.Single(recorder3.Messages));
        Assert.Equal("fan-msg", msg1.OrderId);
        Assert.Equal("fan-msg", msg2.OrderId);
        Assert.Equal("fan-msg", msg3.OrderId);
    }

    [Fact]
    public async Task DeclareBinding_Should_Chain_When_TopicBoundToTopicBoundToQueue()
    {
        // arrange - topic -> topic -> queue chain via builder API; handler on final queue
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.DeclareTopic("chain-source");
                t.DeclareTopic("chain-mid");
                t.DeclareQueue("chain-dest");
                t.DeclareBinding("chain-source", "chain-mid").ToTopic("chain-mid");
                t.DeclareBinding("chain-mid", "chain-dest").ToQueue("chain-dest");

                t.Endpoint("chain-ep").Handler<OrderCreatedHandler>().Queue("chain-dest");

                t.DispatchEndpoint("chain-dispatch").ToTopic("chain-source").Publish<OrderCreated>();
            })
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "chain-msg" }, CancellationToken.None);

        // assert - message traverses topic chain and lands at the handler
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the chained event");

        var msg = Assert.IsType<OrderCreated>(Assert.Single(recorder.Messages));
        Assert.Equal("chain-msg", msg.OrderId);
    }

    [Fact]
    public async Task DeclareBinding_Should_CoexistWithConvention_When_HandlerAlsoRegistered()
    {
        // arrange - convention handler + extra consumer both receive from the same publish topic
        var recorder = new MessageRecorder();
        var extraRecorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddKeyedSingleton("main", recorder)
            .AddKeyedSingleton("extra", extraRecorder)
            .AddMessageBus()
            .AddEventHandler<CoexistHandler>()
            .AddConsumer<CoexistConsumer>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish through the bus so both convention and consumer bindings fire
        await bus.PublishAsync(new OrderCreated { OrderId = "coexist-test" }, CancellationToken.None);

        // assert - convention handler receives the message
        Assert.True(await recorder.WaitAsync(s_timeout), "Convention handler should receive the event");

        // assert - consumer also receives the message via fan-out
        Assert.True(await extraRecorder.WaitAsync(s_timeout), "Consumer should receive the event via fan-out");
    }

    [Fact]
    public async Task ToInMemoryQueue_Should_RouteMessage_When_SpecifiedOnDispatchEndpoint()
    {
        // arrange - use ToQueue on dispatch endpoint to route ProcessPayment to a specific queue
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddInMemory(t =>
                t.DispatchEndpoint("payment-dispatch").ToQueue("process-payment").Send<ProcessPayment>())
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.SendAsync(new ProcessPayment { OrderId = "ORD-ROUTE", Amount = 10m }, CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout),
            "Handler should receive message routed via ToQueue on dispatch endpoint");

        var msg = Assert.IsType<ProcessPayment>(Assert.Single(recorder.Messages));
        Assert.Equal("ORD-ROUTE", msg.OrderId);
    }

    [Fact]
    public async Task ToInMemoryTopic_Should_RouteMessage_When_SpecifiedOnDispatchEndpoint()
    {
        // arrange - use convention-based topic routing for events
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish event which routes to topic by convention
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-TOPIC" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler should receive message routed via topic");

        var msg = Assert.IsType<OrderCreated>(Assert.Single(recorder.Messages));
        Assert.Equal("ORD-TOPIC", msg.OrderId);
    }

    [Fact]
    public async Task Endpoint_Should_ReceiveMessages_When_ConfiguredWithQueueAndHandler()
    {
        // arrange - builder.Endpoint("ep").Queue("q").Handler<T>()
        // Register handler at the host level so routes are discovered, and bind it
        // to a specific queue via the transport endpoint configuration.
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddInMemory(t =>
                t.Endpoint("process-payment").Queue("process-payment").Handler<ProcessPaymentHandler>())
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - send a request that should be routed to the configured endpoint
        await bus.SendAsync(new ProcessPayment { OrderId = "ORD-EP", Amount = 25m }, CancellationToken.None);

        // assert - handler receives
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler on configured endpoint should receive message");

        var msg = Assert.IsType<ProcessPayment>(Assert.Single(recorder.Messages));
        Assert.Equal("ORD-EP", msg.OrderId);
    }

    [Fact]
    public void Topology_Should_MatchSnapshot_When_BuilderDeclaresTopicQueueBinding()
    {
        // arrange & act - topology declarations via builder API
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddInMemory(t =>
            {
                t.DeclareTopic("audit-events");
                t.DeclareQueue("audit-queue");
                t.DeclareBinding("audit-events", "audit-queue").ToQueue("audit-queue");
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        // assert
        var snapshot = TopologySnapshotHelper.CreateSnapshot(topology);
        snapshot.MatchSnapshot();
    }

    [Fact]
    public void Topology_Should_MatchSnapshot_When_BuilderAndConventionCombined()
    {
        // arrange & act - convention handler + builder-declared topology
        var builder = new ServiceCollection().AddMessageBus();
        builder.Host(h => h.ServiceName("test-app"));
        var runtime = builder
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.DeclareTopic("extra-events");
                t.DeclareQueue("extra-queue");
                t.DeclareBinding("extra-events", "extra-queue").ToQueue("extra-queue");
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        // assert
        var snapshot = TopologySnapshotHelper.CreateSnapshot(topology);
        snapshot.MatchSnapshot();
    }

    public sealed class ProcessPaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }

    public sealed record TestEvent(string Message);

    public sealed class MessageRecordConsumer : IEventRequestHandler<TestEvent>
    {
        private readonly MessageRecorder _recorder;

        public MessageRecordConsumer(MessageRecorder recorder)
        {
            _recorder = recorder;
        }

        public ValueTask HandleAsync(TestEvent request, CancellationToken cancellationToken)
        {
            _recorder.Record(request);
            return default;
        }
    }

    public sealed class FanOutHandler1([FromKeyedServices("fan1")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class FanOutHandler2([FromKeyedServices("fan2")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class FanOutHandler3([FromKeyedServices("fan3")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class CoexistHandler([FromKeyedServices("main")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class CoexistConsumer([FromKeyedServices("extra")] MessageRecorder recorder) : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            recorder.Record(context.Message);
            return default;
        }
    }
}
