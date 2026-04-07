using System.Collections.Concurrent;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Consumers;

public sealed class ConsumerBehaviorTests
{
    [Fact]
    public void Describe_Should_ReturnConsumerDescription_When_EventHandlerRegistered()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));

        // act
        var description = consumer.Describe();

        // assert
        description.MatchSnapshot();
    }

    [Fact]
    public async Task ProcessAsync_Should_ThrowInvalidOperationException_When_ContextIsNotConsumeContext()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        var fakeContext = new FakeReceiveContext();

        // act & assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.ProcessAsync(fakeContext).AsTask());
        Assert.Equal("Context is not a handler context", ex.Message);
    }

    [Fact]
    public void Consumers_Should_ContainOnlyReplyConsumer_When_NoHandlersAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(_ => { });

        // assert
        Assert.Single(runtime.Consumers);
        Assert.Equal("Reply", runtime.Consumers.First().Name);
    }

    [Fact]
    public void Consumers_Should_HaveCorrectCount_When_SingleEventHandlerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert - 1 handler + Reply = 2
        Assert.Equal(2, runtime.Consumers.Count);
    }

    [Fact]
    public void Consumers_Should_HaveCorrectCount_When_MultipleHandlersAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.AddEventHandler<ItemShippedHandler>();
            b.AddRequestHandler<ProcessPaymentHandler>();
        });

        // assert - 3 handlers + Reply = 4
        Assert.Equal(4, runtime.Consumers.Count);
    }

    [Fact]
    public void Consumers_Should_AllHaveDistinctNames_When_MultipleHandlersAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.AddEventHandler<ItemShippedHandler>();
            b.AddRequestHandler<ProcessPaymentHandler>();
        });

        // assert
        var names = runtime.Consumers.Select(c => c.Name).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void Identity_Should_BeHandlerType_When_EventHandlerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        Assert.Equal(typeof(OrderCreatedHandler), consumer.Identity);
    }

    [Fact]
    public void Identity_Should_BeHandlerType_When_RequestHandlerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(ProcessPaymentHandler));
        Assert.Equal(typeof(ProcessPaymentHandler), consumer.Identity);
    }

    [Fact]
    public void Initialize_Should_ThrowInvalidOperationException_When_CalledTwice()
    {
        // arrange - get an already-initialized consumer from the runtime
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));

        // act & assert - calling Initialize again should throw before touching the context
        var ex = Assert.Throws<InvalidOperationException>(() => consumer.Initialize(null!));
        Assert.Equal("Handler already initialized", ex.Message);
    }

    [Fact]
    public async Task Consumer_Should_ReceiveTypedContext_When_Consumed()
    {
        // arrange
        var capture = new ConsumeCapture<OrderCreated>();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(capture);
            b.AddConsumer<OrderCreatedConsumer>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-CTX" }, CancellationToken.None);

        // assert
        Assert.True(await capture.WaitAsync(TimeSpan.FromSeconds(10)));
        var msg = Assert.Single(capture.Messages);
        Assert.Equal("ORD-CTX", msg.OrderId);
    }

    [Fact]
    public async Task Consumer_Should_ExposeHeaders_When_Consumed()
    {
        // arrange
        var capture = new ConsumeCapture<OrderCreated>();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(capture);
            b.AddConsumer<OrderCreatedConsumer>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(
            new OrderCreated { OrderId = "ORD-HDR" },
            new PublishOptions { Headers = new() { ["x-test"] = "hello" } },
            CancellationToken.None);

        // assert
        Assert.True(await capture.WaitAsync(TimeSpan.FromSeconds(10)));
        var headers = Assert.Single(capture.CapturedHeaders);
        Assert.True(headers.TryGetValue("x-test", out var val), "Custom header 'x-test' not found");
        Assert.Equal("hello", val);
    }

    /// <summary>
    /// Minimal stub implementing <see cref="IReceiveContext"/> but NOT
    /// <see cref="IConsumeContext"/>. The type check in
    /// <see cref="Consumer.ProcessAsync"/> fails before any property is accessed,
    /// so all properties can safely use null!.
    /// </summary>
    private sealed class FakeReceiveContext : IReceiveContext
    {
        public IHeaders Headers => null!;
        IReadOnlyHeaders IMessageContext.Headers => null!;
        public IFeatureCollection Features => null!;
        public MessagingTransport Transport { get; set; } = null!;
        public ReceiveEndpoint Endpoint { get; set; } = null!;
        public string? MessageId { get; set; }
        public string? CorrelationId { get; set; }
        public string? ConversationId { get; set; }
        public string? CausationId { get; set; }
        public Uri? SourceAddress { get; set; }
        public Uri? DestinationAddress { get; set; }
        public Uri? ResponseAddress { get; set; }
        public Uri? FaultAddress { get; set; }
        public MessageContentType? ContentType { get; set; }
        public MessageType? MessageType { get; set; }
        public DateTimeOffset? SentAt { get; set; }
        public DateTimeOffset? DeliverBy { get; set; }
        public int? DeliveryCount { get; set; }
        public ReadOnlyMemory<byte> Body => Array.Empty<byte>();
        public MessageEnvelope? Envelope { get; set; }
        public IRemoteHostInfo Host { get; set; } = null!;
        public IMessagingRuntime Runtime { get; set; } = null!;
        public CancellationToken CancellationToken { get; set; }
        public IServiceProvider Services { get; set; } = null!;

        public void SetEnvelope(MessageEnvelope envelope) { }
    }

    public sealed class OrderCreated
    {
        public string OrderId { get; init; } = "";
    }

    public sealed class ItemShipped
    {
        public string TrackingNumber { get; init; } = "";
    }

    public sealed class ProcessPayment
    {
        public string OrderId { get; init; } = "";
        public decimal Amount { get; init; }
    }

    public sealed class OrderCreatedHandler : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken) => default;
    }

    public sealed class ItemShippedHandler : IEventHandler<ItemShipped>
    {
        public ValueTask HandleAsync(ItemShipped message, CancellationToken cancellationToken) => default;
    }

    public sealed class ProcessPaymentHandler : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken) => default;
    }

    public sealed class ConsumeCapture<T>
    {
        private readonly SemaphoreSlim _semaphore = new(0);
        public ConcurrentBag<T> Messages { get; } = [];
        public ConcurrentBag<Dictionary<string, object?>> CapturedHeaders { get; } = [];

        public void Record(IConsumeContext<T> context)
        {
            Messages.Add(context.Message);
            var dict = new Dictionary<string, object?>();
            foreach (var h in context.Headers)
            {
                dict[h.Key] = h.Value;
            }
            CapturedHeaders.Add(dict);
            _semaphore.Release();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount = 1)
        {
            for (var i = 0; i < expectedCount; i++)
            {
                if (!await _semaphore.WaitAsync(timeout))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public sealed class OrderCreatedConsumer(ConsumeCapture<OrderCreated> capture) : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            capture.Record(context);
            return default;
        }
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }

    private static async Task<ServiceProvider> CreateBusAsync(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }
}
