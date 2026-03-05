using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Consumers.Batching;

public sealed class BatchConsumerRegistrationTests
{
    [Fact]
    public void AddBatchHandler_Should_RegisterConsumerWithHandlerTypeName_When_BatchHandlerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddBatchHandler<TestBatchHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(TestBatchHandler));
        Assert.NotNull(consumer);
    }

    [Fact]
    public void AddBatchHandler_Should_SetConsumerIdentityToHandlerType_When_BatchHandlerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddBatchHandler<TestBatchHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(TestBatchHandler));
        Assert.Equal(typeof(TestBatchHandler), consumer.Identity);
    }

    [Fact]
    public void AddBatchHandler_Should_RegisterSubscribeRoute_When_BatchHandlerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddBatchHandler<TestBatchHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(TestBatchHandler));
        var routes = runtime.Router.GetInboundByConsumer(consumer);
        var route = Assert.Single(routes);
        Assert.Equal(InboundRouteKind.Subscribe, route.Kind);
    }

    [Fact]
    public void AddBatchHandler_Should_RegisterCorrectMessageType_When_RouteCreated()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddBatchHandler<TestBatchHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(TestBatchHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.Equal(typeof(TestEvent), route.MessageType!.RuntimeType);
    }

    [Fact]
    public void AddBatchHandler_Should_ThrowArgumentOutOfRange_When_InvalidBatchOptions()
    {
        // arrange & act & assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateRuntime(b => b.AddBatchHandler<TestBatchHandler>(opts => opts.MaxBatchSize = 0))
        );
    }

    [Fact]
    public void AddBatchHandler_Should_CoexistWithEventHandler_When_DifferentEventTypes()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.AddBatchHandler<TestBatchHandler>();
            b.AddEventHandler<TestEventHandler>();
        });

        // assert — batch handler + event handler + reply consumer
        Assert.Equal(3, runtime.Consumers.Count);
    }

    [Fact]
    public void AddHandler_Should_AutoDetectBatchHandler_When_IBatchEventHandlerRegistered()
    {
        // arrange & act — use AddHandler (auto-detect) on the message bus builder directly
        var runtime = CreateRuntime(b => b.ConfigureMessageBus(mb => mb.AddHandler<TestBatchHandler>()));

        // assert — should be registered as a BatchConsumer
        var consumer = runtime.Consumers.First(c => c.Name == nameof(TestBatchHandler));
        Assert.NotNull(consumer);
        Assert.IsType<BatchConsumer<TestBatchHandler, TestEvent>>(consumer);
    }

    // --- Helpers ---

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }

    // --- Test types ---

    public sealed class TestEvent
    {
        public required string Id { get; init; }
    }

    public sealed class OtherEvent
    {
        public required string Name { get; init; }
    }

    public sealed class TestBatchHandler : IBatchEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(IMessageBatch<TestEvent> batch, CancellationToken cancellationToken) => default;
    }

    public sealed class TestEventHandler : IEventHandler<OtherEvent>
    {
        public ValueTask HandleAsync(OtherEvent message, CancellationToken cancellationToken) => default;
    }
}
