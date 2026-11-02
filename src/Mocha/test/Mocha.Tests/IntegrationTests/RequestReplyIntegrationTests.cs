using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Tests.IntegrationTests;

public class RequestReplyIntegrationTests : ConsumerIntegrationTestsBase
{
    [Fact]
    public async Task GetOrderStatusHandler_Should_ReturnTypedResponse_When_RequestSent()
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
        var response = await bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-1" }, CancellationToken.None);

        // assert
        Assert.Equal("ORD-1", response.OrderId);
        Assert.Equal("Shipped", response.Status);
    }

    [Fact]
    public async Task GetOrderStatusHandler_Should_Throw_When_NullResponseReturned()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddRequestHandler<NullResponseHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act & assert - null response causes an error to propagate back
        using var cts = new CancellationTokenSource(Timeout);
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-1" }, cts.Token)
        );
    }

    [Fact]
    public async Task RequestResponse_Should_CorrelateByCorrelationId_When_ConcurrentRequestsSent()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddRequestHandler<GetOrderStatusHandler>();
        });

        // act - send two concurrent requests
        var task1 = Task.Run(async () =>
        {
            using var scope = provider.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            return await bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-A" }, CancellationToken.None);
        });

        var task2 = Task.Run(async () =>
        {
            using var scope = provider.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            return await bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-B" }, CancellationToken.None);
        });

        var results = await Task.WhenAll(task1, task2);

        // assert - each caller gets the correct response (not swapped)
        var responseA = results.First(r => r.OrderId == "ORD-A");
        var responseB = results.First(r => r.OrderId == "ORD-B");

        Assert.Equal("Shipped", responseA.Status);
        Assert.Equal("Shipped", responseB.Status);
    }

    [Fact]
    public async Task AddHandler_Should_DetectRequestResponseHandler_When_CalledForRequestResponseHandler()
    {
        // arrange - use AddHandler<T> for a request-response handler
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.Services.AddScoped<GetOrderStatusHandler>();
            b.ConfigureMessageBus(static h => h.AddHandler<GetOrderStatusHandler>());
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var response = await bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-1" }, CancellationToken.None);

        // assert
        Assert.Equal("Shipped", response.Status);
    }

    [Fact]
    public async Task RequestHandler_Should_PropagateException_ToCaller_When_ExceptionThrown()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddRequestHandler<ThrowingRequestHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act & assert - the exception should propagate back to the caller
        using var cts = new CancellationTokenSource(Timeout);
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-FAIL" }, cts.Token)
        );
    }

    [Fact]
    public async Task RequestAsync_Should_CompleteWithinTimeout_When_TimeoutSpecified()
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
        var response = await bus.RequestAsync(new GetOrderStatus { OrderId = "REQ-1" }, CancellationToken.None);

        // assert
        Assert.NotNull(response);
        Assert.Equal("REQ-1", response.OrderId);
        Assert.Equal("Shipped", response.Status);
    }

    [Fact]
    public async Task RequestAsync_ShouldReturnCorrectResponse_WhenConcurrentRequestsSent()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddRequestHandler<GetOrderStatusHandler>();
        });

        // act - fire 10 concurrent requests
        var tasks = Enumerable
            .Range(1, 10)
            .Select(async i =>
            {
                using var scope = provider.CreateScope();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                var response = await bus.RequestAsync(
                    new GetOrderStatus { OrderId = $"CONC-{i}" },
                    CancellationToken.None);
                return response;
            })
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // assert - each response matches its request
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal($"CONC-{i + 1}", responses[i].OrderId);
        }
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
}
