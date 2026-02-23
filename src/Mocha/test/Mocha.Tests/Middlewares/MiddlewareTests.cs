using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class MiddlewareTests
{
    [Fact]
    public void Runtime_Should_NotBeStarted_When_JustBuilt()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        Assert.False(runtime.IsStarted);
    }

    [Fact]
    public async Task Runtime_Should_BeStarted_When_StartAsyncCalled()
    {
        // arrange
        await using var provider = await CreateBusAsync(b => b.AddEventHandler<OrderCreatedHandler>());

        // act
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

        // assert
        Assert.True(runtime.IsStarted);
    }

    [Fact]
    public async Task PublishAsync_Should_FlowEventThroughMiddleware_When_EventPublished()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<OrderCreatedHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "MW-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout), "Event did not flow through middleware pipeline");

        var message = Assert.IsType<OrderCreated>(Assert.Single(recorder.Messages));
        Assert.Equal("MW-1", message.OrderId);
    }

    [Fact]
    public async Task RequestAsync_Should_FlowRequestResponseThroughMiddleware_When_RequestMade()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddRequestHandler<GetOrderStatusHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var response = await bus.RequestAsync(new GetOrderStatus { OrderId = "MW-2" }, CancellationToken.None);

        // assert
        Assert.Equal("Shipped", response.Status);
        Assert.Equal("MW-2", response.OrderId);
    }

    [Fact]
    public async Task RequestAsync_Should_FlowRequestThroughMiddleware_When_SendRequestCalled()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddRequestHandler<ProcessPaymentHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.RequestAsync(new ProcessPayment { Amount = 42.00m }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout), "Request did not flow through middleware pipeline");

        var message = Assert.IsType<ProcessPayment>(Assert.Single(recorder.Messages));
        Assert.Equal(42.00m, message.Amount);
    }

    [Fact]
    public async Task DefaultMessageBus_Should_BeRegistered_When_BuiltWithAddMessageBus()
    {
        // arrange
        await using var provider = await CreateBusAsync(b => b.AddEventHandler<OrderCreatedHandler>());

        using var scope = provider.CreateScope();

        // act
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // assert — bus is a DefaultMessageBus registered by AddMessageBus
        Assert.IsType<DefaultMessageBus>(bus);
    }

    public sealed class OrderCreated
    {
        public string OrderId { get; init; } = "";
    }

    public sealed class ProcessPayment
    {
        public decimal Amount { get; init; }
    }

    public sealed class GetOrderStatus : IEventRequest<OrderStatusResponse>
    {
        public string OrderId { get; init; } = "";
    }

    public sealed class OrderStatusResponse
    {
        public string OrderId { get; init; } = "";
        public string Status { get; init; } = "";
    }

    public sealed class OrderCreatedHandler(MessageRecorder recorder) : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
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

    public sealed class GetOrderStatusHandler(MessageRecorder recorder)
        : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
    {
        public ValueTask<OrderStatusResponse> HandleAsync(GetOrderStatus request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return new(new OrderStatusResponse { OrderId = request.OrderId, Status = "Shipped" });
        }
    }

    [Fact]
    public async Task PublishAsync_Should_NotDeliverEvent_When_TokenCancelled()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<OrderCreatedHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // pre-cancel

        // act & assert - the framework may throw OperationCanceledException
        // or TaskCanceledException, or it may silently skip delivery.
        // We verify the event is not recorded by the handler either way.
        try
        {
            await bus.PublishAsync(new OrderCreated { OrderId = "cancelled" }, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // expected path
        }

        // Task.Delay: negative wait — proves the cancelled event was NOT delivered
        await Task.Delay(200, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(recorder.Messages, m => m is OrderCreated oc && oc.OrderId == "cancelled");
    }

    [Fact]
    public async Task RequestAsync_Should_ThrowOrNotDeliver_When_TokenCancelled()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddRequestHandler<GetOrderStatusHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // pre-cancel

        // act & assert - request with cancelled token should throw or not deliver
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await bus.RequestAsync(new GetOrderStatus { OrderId = "cancelled-req" }, cts.Token)
        );
    }

    [Fact]
    public async Task Runtime_Should_SetIsStarted_When_StartAsyncCalled()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
            b.AddEventHandler<OrderCreatedHandler>());

        // assert - CreateBusAsync calls StartAsync internally
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        Assert.True(runtime.IsStarted);
    }

    [Fact]
    public async Task Runtime_Should_CompleteWithoutError_When_DisposeAsyncCalled()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
            b.AddEventHandler<OrderCreatedHandler>());

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        Assert.True(runtime.IsStarted);

        // act & assert — dispose completes without throwing.
        // No observable state change beyond clean disposal; the runtime
        // does not expose a "disposed" flag.
        await runtime.DisposeAsync();
    }

    [Fact]
    public async Task Pipeline_Should_ProcessAllEvents_When_MultipleEventsPublished()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<OrderCreatedHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish 5 events
        for (var i = 0; i < 5; i++)
        {
            await bus.PublishAsync(new OrderCreated { OrderId = $"msg-{i}" }, CancellationToken.None);
        }

        // assert
        Assert.True(await recorder.WaitAsync(Timeout, expectedCount: 5));
        Assert.Equal(5, recorder.Messages.Count);
    }

    [Fact]
    public async Task Pipeline_Should_ProcessAllEventsWhenConcurrent_When_ConcurrentPublishCalled()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<OrderCreatedHandler>();
        });

        // act - publish 10 events concurrently
        var tasks = Enumerable
            .Range(0, 10)
            .Select(async i =>
            {
                using var scope = provider.CreateScope();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                await bus.PublishAsync(new OrderCreated { OrderId = $"concurrent-{i}" }, CancellationToken.None);
            });

        await Task.WhenAll(tasks);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout, expectedCount: 10));
        Assert.Equal(10, recorder.Messages.Count);
    }

    [Fact]
    public async Task Pipeline_Should_ProcessAllRequestsWhenConcurrent_When_ConcurrentRequestsCalled()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddRequestHandler<GetOrderStatusHandler>();
        });

        // act - send 10 requests concurrently
        var tasks = Enumerable
            .Range(0, 10)
            .Select(async i =>
            {
                using var scope = provider.CreateScope();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                var response = await bus.RequestAsync(
                    new GetOrderStatus { OrderId = $"concurrent-req-{i}" },
                    CancellationToken.None);
                return response;
            });

        var responses = await Task.WhenAll(tasks);

        // assert
        Assert.Equal(10, responses.Length);
        Assert.All(responses, r => Assert.Equal("Shipped", r.Status));
    }

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

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
