using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class MessageBusHostBuilderMiddlewareTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task UseDispatch_Should_PlaceMiddlewareByAnchor_When_BeforeOrAfterIsSpecified()
    {
        // arrange
        var order = new ConcurrentQueue<string>();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(
            recorder,
            builder => builder
                .UseDispatch(CreateDispatch("anchor", order))
                .UseDispatch(CreateDispatch("before", order), before: "anchor")
                .UseDispatch(CreateDispatch("after", order), after: "anchor"));
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated("1"), CancellationToken.None);

        // assert
        Assert.Equal(["before", "anchor", "after"], order);
    }

    [Fact]
    public async Task UseReceive_Should_PlaceMiddlewareByAnchor_When_BeforeOrAfterIsSpecified()
    {
        // arrange
        var order = new ConcurrentQueue<string>();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(
            recorder,
            builder => builder
                .UseReceive(CreateReceive("anchor", order))
                .UseReceive(CreateReceive("before", order), before: "anchor")
                .UseReceive(CreateReceive("after", order), after: "anchor"));
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated("1"), CancellationToken.None);
        var handled = await recorder.WaitAsync(s_timeout);

        // assert
        Assert.True(handled);
        Assert.Equal(["before", "anchor", "after"], order);
    }

    [Fact]
    public async Task UseConsume_Should_PlaceMiddlewareByAnchor_When_BeforeOrAfterIsSpecified()
    {
        // arrange
        var order = new ConcurrentQueue<string>();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(
            recorder,
            builder => builder
                .UseConsume(CreateConsume("anchor", order))
                .UseConsume(CreateConsume("before", order), before: "anchor")
                .UseConsume(CreateConsume("after", order), after: "anchor"));
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated("1"), CancellationToken.None);
        var handled = await recorder.WaitAsync(s_timeout);

        // assert
        Assert.True(handled);
        Assert.Equal(["before", "anchor", "after"], order);
    }

    private static DispatchMiddlewareConfiguration CreateDispatch(string key, ConcurrentQueue<string> order)
        => new(
            (_, next) => context =>
            {
                order.Enqueue(key);
                return next(context);
            },
            key);

    private static ReceiveMiddlewareConfiguration CreateReceive(string key, ConcurrentQueue<string> order)
        => new(
            (_, next) => context =>
            {
                order.Enqueue(key);
                return next(context);
            },
            key);

    private static ConsumerMiddlewareConfiguration CreateConsume(string key, ConcurrentQueue<string> order)
        => new(
            (_, next) => context =>
            {
                order.Enqueue(key);
                return next(context);
            },
            key);

    private static async Task<ServiceProvider> CreateBusAsync(
        MessageRecorder recorder,
        Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddEventHandler<OrderCreatedHandler>().AddInMemory();

        var provider = services.BuildServiceProvider();
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(CancellationToken.None);
        return provider;
    }

    public sealed record OrderCreated(string OrderId);

    public sealed class OrderCreatedHandler(MessageRecorder recorder) : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }
}
