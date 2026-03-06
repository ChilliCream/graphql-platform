using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests;

public class HandlerResolutionTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task AddEventHandler_Should_ResolveDependency_When_HandlerHasDIConstructor()
    {
        // arrange
        var counter = new InvocationCounter();
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(counter)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<DependencyHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the event");

        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task AddHandler_Should_CreateSubscribeConsumer_When_EventHandlerRegistered()
    {
        // arrange - use ConfigureMessageBus to call AddHandler<T> directly
        var recorder = new MessageRecorder();
        var builder = new ServiceCollection().AddSingleton(recorder).AddScoped<OrderCreatedHandler>().AddMessageBus();
        builder.ConfigureMessageBus(static h => h.AddHandler<OrderCreatedHandler>());
        await using var provider = await builder.AddInMemory().BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout),
            "Handler was not called - AddHandler did not create SubscribeConsumer");

        var message = Assert.IsType<OrderCreated>(Assert.Single(recorder.Messages));
        Assert.Equal("ORD-1", message.OrderId);
    }

    [Fact]
    public async Task AddHandler_Should_CreateSendConsumer_When_RequestHandlerRegistered()
    {
        // arrange - use AddHandler<T> for a fire-and-forget request handler
        var recorder = new MessageRecorder();
        var builder = new ServiceCollection().AddSingleton(recorder).AddScoped<ProcessPaymentHandler>().AddMessageBus();
        builder.ConfigureMessageBus(static h => h.AddHandler<ProcessPaymentHandler>());
        await using var provider = await builder.AddInMemory().BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.RequestAsync(new ProcessPayment { OrderId = "ORD-1", Amount = 10m }, CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout),
            "Handler was not called - AddHandler did not create SendConsumer");
    }

    [Fact]
    public async Task AddHandler_Should_CreateRequestConsumer_When_ResponseHandlerRegistered()
    {
        // arrange - use AddHandler<T> for a request-response handler
        var recorder = new MessageRecorder();
        var builder = new ServiceCollection().AddSingleton(recorder).AddScoped<GetOrderStatusHandler>().AddMessageBus();
        builder.ConfigureMessageBus(static h => h.AddHandler<GetOrderStatusHandler>());
        await using var provider = await builder.AddInMemory().BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var response = await bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-1" }, CancellationToken.None);

        // assert
        Assert.Equal("Shipped", response.Status);
    }

    public sealed class InvocationCounter
    {
        private int _count;

        public int Count => _count;

        public void Increment() => Interlocked.Increment(ref _count);
    }

    public sealed class DependencyHandler(InvocationCounter counter, MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            counter.Increment();
            recorder.Record(message);
            return default;
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
}
