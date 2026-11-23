using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Mocha.Events;

namespace Mocha.Tests.IntegrationTests;

public sealed class RequestTimeoutIntegrationTests : ConsumerIntegrationTestsBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequestAsync_Should_CompleteByDeadline_When_ResponseArrivesBeforeDispatchCompletes(bool fault)
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var responseReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var provider = await CreateBusAsync(builder =>
        {
            builder.Services.AddSingleton<TimeProvider>(timeProvider);
            builder.Services.AddSingleton(new MessageRecorder());
            if (fault)
            {
                builder.AddRequestHandler<ThrowingRequestHandler>();
            }
            else
            {
                builder.AddRequestHandler<GetOrderStatusHandler>();
            }

            builder.ConfigureMessageBus(b => b.UseDispatch(new DispatchMiddlewareConfiguration(
                (_, next) => async context =>
                {
                    if (context.Message is not GetOrderStatus)
                    {
                        await next(context);
                        return;
                    }

                    var response = context.Services.GetRequiredService<DeferredResponseManager>()
                        .GetPromise(context.CorrelationId!);
                    await next(context);
                    try
                    {
                        await response;
                    }
                    catch (RemoteErrorException) when (fault)
                    {
                    }

                    responseReceived.TrySetResult();
                    await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, context.CancellationToken);
                },
                "DelayRequestDispatchCompletion")));
        });
        using var scope = provider.CreateScope();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var request = bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-1" }, cancellation.Token).AsTask();

        try
        {
            await responseReceived.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.False(request.IsCompleted);

            // act
            timeProvider.Advance(TimeSpan.FromMinutes(2));

            // assert
            if (fault)
            {
                await Assert.ThrowsAsync<RemoteErrorException>(
                    () => request.WaitAsync(Timeout, TestContext.Current.CancellationToken));
            }
            else
            {
                var response = await request.WaitAsync(Timeout, TestContext.Current.CancellationToken);
                Assert.Equal("ORD-1", response.OrderId);
                Assert.Equal("Shipped", response.Status);
            }
        }
        finally
        {
            await cancellation.CancelAsync();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequestAsync_Should_ReleasePendingResponse_When_DispatchFailsOrIsCancelled(bool cancel)
    {
        // arrange
        var started = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var provider = await CreateBusAsync(builder =>
        {
            builder.Services.AddSingleton(new MessageRecorder());
            builder.AddRequestHandler<GetOrderStatusHandler>();
            builder.ConfigureMessageBus(b => b.UseDispatch(new DispatchMiddlewareConfiguration(
                (_, _) => async context =>
                {
                    started.TrySetResult(context.CorrelationId!);
                    if (cancel)
                    {
                        await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, context.CancellationToken);
                    }

                    throw new InvalidOperationException("Dispatch failed.");
                },
                "FailDispatch")));
        });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var responses = provider.GetRequiredService<DeferredResponseManager>();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        // act
        var request = bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-1" }, cancellation.Token).AsTask();
        var correlationId = await started.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        if (cancel)
        {
            await cancellation.CancelAsync();
        }

        // assert
        if (cancel)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => request.WaitAsync(Timeout, TestContext.Current.CancellationToken));
        }
        else
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => request.WaitAsync(Timeout, TestContext.Current.CancellationToken));
            Assert.Equal("Dispatch failed.", exception.Message);
        }

        Assert.False(responses.CompletePromise(correlationId, new OrderStatusResponse { OrderId = "ORD-1", Status = "Late" }));
    }

    [Fact]
    public async Task RequestAsync_Should_Timeout_When_DispatchCompletesWithoutAResponse()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var dispatched = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var provider = await CreateBusAsync(builder =>
        {
            builder.Services.AddSingleton<TimeProvider>(timeProvider);
            builder.Services.AddSingleton(new MessageRecorder());
            builder.AddRequestHandler<GetOrderStatusHandler>();
            builder.ConfigureMessageBus(b => b.UseDispatch(new DispatchMiddlewareConfiguration(
                (_, _) => context =>
                {
                    dispatched.TrySetResult(context.CorrelationId!);
                    return default;
                },
                "DiscardRequest")));
        });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var responses = provider.GetRequiredService<DeferredResponseManager>();

        // act
        var request = bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-1" }, TestContext.Current.CancellationToken).AsTask();
        var correlationId = await dispatched.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeSpan.FromMinutes(2));

        // assert
        var exception = await Assert.ThrowsAsync<ResponseTimeoutException>(
            () => request.WaitAsync(Timeout, TestContext.Current.CancellationToken));
        Assert.Equal(correlationId, exception.CorrelationId);
        Assert.Equal(TimeSpan.FromMinutes(2), exception.Timeout);
        Assert.False(responses.CompletePromise(correlationId, "late"));
    }
}
