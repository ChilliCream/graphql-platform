using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Tests.Middlewares.Receive;

public sealed class ConcurrencyLimiterMiddlewareTests : ReceiveMiddlewareTestBase
{
    [Fact]
    public async Task InvokeAsync_Should_ProcessMessage_When_SingleMessageSent()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddConcurrencyLimiter(o => o.MaxConcurrency = 2);
            b.AddEventHandler<SimpleHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TestEvent { Id = "single-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout));
        Assert.Single(recorder.Messages);
    }

    [Fact]
    public async Task InvokeAsync_Should_ProcessMessageSuccessfully_When_DefaultConcurrencyUsed()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddConcurrencyLimiter(_ => { });
            b.AddEventHandler<SimpleHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TestEvent { Id = "default-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout));
        Assert.Single(recorder.Messages);
    }

    [Fact]
    public async Task InvokeAsync_Should_ProcessAllConcurrently_When_WithinConcurrencyLimit()
    {
        // arrange
        var concurrencyTracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(concurrencyTracker);
            b.Services.AddSingleton(recorder);
            b.AddConcurrencyLimiter(o => o.MaxConcurrency = 5);
            b.AddEventHandler<SlowConcurrencyTrackingHandler>();
        });

        // act

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        _ = bus.PublishAsync(new TestEvent { Id = "within-1" }, CancellationToken.None);
        _ = bus.PublishAsync(new TestEvent { Id = "within-2" }, CancellationToken.None);
        _ = bus.PublishAsync(new TestEvent { Id = "within-3" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout, expectedCount: 3));
        Assert.Equal(3, recorder.Messages.Count);
        Assert.True(
            concurrencyTracker.PeakConcurrency <= 5,
            $"Expected max 5 concurrent, but observed {concurrencyTracker.PeakConcurrency}");
    }

    [Fact]
    public async Task InvokeAsync_Should_AllowFullConcurrency_When_ExactlyAtLimit()
    {
        // arrange
        var concurrencyTracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(concurrencyTracker);
            b.Services.AddSingleton(recorder);
            b.AddConcurrencyLimiter(o => o.MaxConcurrency = 3);
            b.AddEventHandler<SlowConcurrencyTrackingHandler>();
        });

        // act
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(new TestEvent { Id = "exact-1" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "exact-2" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "exact-3" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout, expectedCount: 3));
        Assert.Equal(3, recorder.Messages.Count);
    }

    [Fact]
    public async Task InvokeAsync_Should_LimitConcurrency_When_MaxConcurrencySet()
    {
        // arrange
        var concurrencyTracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(concurrencyTracker);
            b.Services.AddSingleton(recorder);
            b.AddConcurrencyLimiter(o => o.MaxConcurrency = 2);
            b.AddEventHandler<SlowConcurrencyTrackingHandler>();
        });

        // act
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(new TestEvent { Id = "limited-1" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "limited-2" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "limited-3" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "limited-4" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "limited-5" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout, expectedCount: 5));
        Assert.Equal(5, recorder.Messages.Count);
        // Critical: max observed concurrency should never exceed the limit
        Assert.True(
            concurrencyTracker.PeakConcurrency <= 2,
            $"Expected max 2 concurrent, but observed {concurrencyTracker.PeakConcurrency}");
    }

    [Fact]
    public async Task InvokeAsync_Should_ProcessInBatches_When_ManyMessagesExceedLimit()
    {
        // arrange
        var concurrencyTracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(concurrencyTracker);
            b.Services.AddSingleton(recorder);
            b.AddConcurrencyLimiter(o => o.MaxConcurrency = 3);
            b.AddEventHandler<SlowConcurrencyTrackingHandler>();
        });

        // act
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(new TestEvent { Id = "batch-1" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "batch-2" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "batch-3" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "batch-4" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "batch-5" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "batch-6" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "batch-7" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "batch-8" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "batch-9" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "batch-10" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout, expectedCount: 10));
        Assert.Equal(10, recorder.Messages.Count);
        Assert.True(
            concurrencyTracker.PeakConcurrency <= 3,
            $"Expected max 3 concurrent, but observed {concurrencyTracker.PeakConcurrency}");
    }

    [Fact]
    public async Task InvokeAsync_Should_EnforceConcurrencyOfOne_When_MaxConcurrencyIsOne()
    {
        // arrange
        var concurrencyTracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(concurrencyTracker);
            b.Services.AddSingleton(recorder);
            b.AddConcurrencyLimiter(o => o.MaxConcurrency = 1);
            b.AddEventHandler<SlowConcurrencyTrackingHandler>();
        });

        // act
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(new TestEvent { Id = "serial-1" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "serial-2" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "serial-3" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "serial-4" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "serial-5" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout, expectedCount: 5));
        Assert.Equal(5, recorder.Messages.Count);
        Assert.Equal(1, concurrencyTracker.PeakConcurrency);
    }

    [Fact]
    public async Task InvokeAsync_Should_BypassLimiter_When_ExplicitlyDisabled()
    {
        // arrange - disable the concurrency limiter
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddConcurrencyLimiter(o =>
            {
                o.Enabled = false;
                o.MaxConcurrency = 1; // Would be very restrictive if enabled
            });
            b.AddEventHandler<SimpleHandler>();
        });

        // act - publish multiple messages
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(new TestEvent { Id = "disabled-1" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "disabled-2" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "disabled-3" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "disabled-4" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "disabled-5" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout, expectedCount: 5));
        Assert.Equal(5, recorder.Messages.Count);
    }

    [Fact]
    public async Task InvokeAsync_Should_ProcessNormally_When_DisabledWithoutSettingMaxConcurrency()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddConcurrencyLimiter(o => o.Enabled = false);
            b.AddEventHandler<SimpleHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TestEvent { Id = "disabled-no-limit" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout));
        Assert.Single(recorder.Messages);
    }

    [Fact]
    public async Task InvokeAsync_Should_ReleaseSemaphore_When_HandlerThrows()
    {
        // arrange - use ID-based throwing so retries of the "throws" message
        // keep throwing (a mutable flag was flaky because the transport retried
        // the message after the flag was cleared, recording it unexpectedly).
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.Services.AddSingleton<ConditionalThrowHandler>();
            b.AddConcurrencyLimiter(o => o.MaxConcurrency = 1);
            b.AddEventHandler<ConditionalThrowHandler>();
        });

        var handler = provider.GetRequiredService<ConditionalThrowHandler>();
        handler.ThrowForIds.Add("throws");

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - first message throws, semaphore should still be released
        await bus.PublishAsync(new TestEvent { Id = "throws" }, CancellationToken.None);

        // No deterministic signal for a swallowed exception; let the fault settle.
        await Task.Delay(200, CancellationToken.None);

        // Second message should be processed (proving semaphore was released)
        await bus.PublishAsync(new TestEvent { Id = "succeeds" }, CancellationToken.None);

        // assert - second message should be processed
        Assert.True(await recorder.WaitAsync(Timeout));
        var recorded = Assert.Single(recorder.Messages);
        Assert.Equal("succeeds", ((TestEvent)recorded).Id);
    }

    [Fact]
    public async Task InvokeAsync_Should_ContinueProcessing_When_MultipleFailuresThenSuccess()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.Services.AddSingleton<ConditionalThrowHandler>();
            b.AddConcurrencyLimiter(o => o.MaxConcurrency = 1);
            b.AddEventHandler<ConditionalThrowHandler>();
        });

        var handler = provider.GetRequiredService<ConditionalThrowHandler>();
        handler.ThrowForIds.Add("fail-1");
        handler.ThrowForIds.Add("fail-2");

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - send failing messages then succeeding ones
        await bus.PublishAsync(new TestEvent { Id = "fail-1" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "fail-2" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "success-1" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Id = "success-2" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout, expectedCount: 2));
        Assert.Equal(2, recorder.Messages.Count);
        Assert.Contains(recorder.Messages, m => ((TestEvent)m).Id == "success-1");
        Assert.Contains(recorder.Messages, m => ((TestEvent)m).Id == "success-2");
    }

    [Fact]
    public async Task Dispose_Should_ThrowObjectDisposed_When_InvokeAsyncCalledAfterDispose()
    {
        // arrange
        var middleware = new ConcurrencyLimiterMiddleware(5);
        middleware.Dispose();

        var context = new StubReceiveContext();
        ReceiveDelegate next = _ => ValueTask.CompletedTask;

        // act & assert - InvokeAsync should throw because the semaphore is disposed
        await Assert.ThrowsAsync<ObjectDisposedException>(() => middleware.InvokeAsync(context, next).AsTask());
    }

    [Fact]
    public async Task InvokeAsync_Should_LimitConcurrency_When_UsingRequestResponse()
    {
        // arrange
        var concurrencyTracker = new ConcurrencyTracker();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(concurrencyTracker);
            b.AddConcurrencyLimiter(o => o.MaxConcurrency = 2);
            b.AddRequestHandler<SlowRequestHandler>();
        });

        // act - send multiple requests concurrently
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var response1 = bus.RequestAsync(new TestRequest { Id = "req-1" }, CancellationToken.None);
        var response2 = bus.RequestAsync(new TestRequest { Id = "req-2" }, CancellationToken.None);
        var response3 = bus.RequestAsync(new TestRequest { Id = "req-3" }, CancellationToken.None);
        var response4 = bus.RequestAsync(new TestRequest { Id = "req-4" }, CancellationToken.None);
        var response5 = bus.RequestAsync(new TestRequest { Id = "req-5" }, CancellationToken.None);

        await Task.WhenAll(
            response1.AsTask(),
            response2.AsTask(),
            response3.AsTask(),
            response4.AsTask(),
            response5.AsTask());

        // assert
        Assert.True(
            concurrencyTracker.PeakConcurrency <= 2,
            $"Expected max 2 concurrent, but observed {concurrencyTracker.PeakConcurrency}");
    }

    /// <summary>
    /// Simple handler that records messages without delay.
    /// </summary>
    private sealed class SimpleHandler(MessageRecorder recorder) : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken ct)
        {
            recorder.Record(message);
            return default;
        }
    }

    /// <summary>
    /// Handler that tracks concurrency and introduces a delay to allow concurrent execution to overlap.
    /// </summary>
    private sealed class SlowConcurrencyTrackingHandler(ConcurrencyTracker concurrencyTracker, MessageRecorder recorder)
        : IEventHandler<TestEvent>
    {
        public async ValueTask HandleAsync(TestEvent message, CancellationToken ct)
        {
            concurrencyTracker.Enter();
            try
            {
                // Small delay to ensure concurrent handlers overlap
                await Task.Delay(100, ct);
                recorder.Record(message);
            }
            finally
            {
                concurrencyTracker.Exit();
            }
        }
    }

    /// <summary>
    /// Handler that blocks until explicitly released. Used to test semaphore waiting behavior.
    /// </summary>
    private sealed class BlockingHandler(MessageRecorder recorder) : IEventHandler<TestEvent>
    {
        private readonly TaskCompletionSource _started = new();
        private readonly TaskCompletionSource _release = new();

        public async ValueTask HandleAsync(TestEvent message, CancellationToken ct)
        {
            _started.TrySetResult();
            await _release.Task;
            recorder.Record(message);
        }

        public Task WaitForStartAsync(TimeSpan timeout)
        {
            return _started.Task.WaitAsync(timeout);
        }

        public void Release()
        {
            _release.TrySetResult();
        }
    }

    /// <summary>
    /// Handler that throws for specific message IDs.
    /// </summary>
    private sealed class ConditionalThrowHandler(MessageRecorder recorder) : IEventHandler<TestEvent>
    {
        public ConcurrentBag<string> ThrowForIds { get; } = [];

        public ValueTask HandleAsync(TestEvent message, CancellationToken ct)
        {
            if (ThrowForIds.Contains(message.Id))
            {
                throw new InvalidOperationException($"Configured to throw for {message.Id}");
            }

            recorder.Record(message);
            return default;
        }
    }

    /// <summary>
    /// Request handler that tracks concurrency with delays.
    /// </summary>
    private sealed class SlowRequestHandler(ConcurrencyTracker concurrencyTracker)
        : IEventRequestHandler<TestRequest, TestResponse>
    {
        public async ValueTask<TestResponse> HandleAsync(TestRequest request, CancellationToken ct)
        {
            concurrencyTracker.Enter();
            try
            {
                await Task.Delay(100, ct);
                return new TestResponse { Id = request.Id, Result = "Processed" };
            }
            finally
            {
                concurrencyTracker.Exit();
            }
        }
    }
}
