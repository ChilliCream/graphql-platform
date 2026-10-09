using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Behaviors;

public class BatchingTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Handler_Should_ReceiveBatch_When_SingleMessageSizeTrigger()
    {
        // arrange - MaxBatchSize=1 so each message immediately triggers a batch
        var recorder = new BatchMessageRecorder();
        await using var provider = await InMemoryBusFixture.CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddBatchHandler<TestBatchHandler>(opts => opts.MaxBatchSize = 1);
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Batch handler was not invoked within timeout");

        var batch = Assert.IsAssignableFrom<IMessageBatch<OrderCreated>>(Assert.Single(recorder.Batches));
        Assert.Single(batch);
        Assert.Equal(BatchCompletionMode.Size, batch.CompletionMode);
        Assert.Equal("1", batch[0].OrderId);
    }

    [Fact]
    public async Task Handler_Should_ReceiveBatch_When_TimeoutExpires()
    {
        // arrange - high max size so only the timer triggers dispatch
        var recorder = new BatchMessageRecorder();
        await using var provider = await InMemoryBusFixture.CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddBatchHandler<TestBatchHandler>(opts =>
            {
                opts.MaxBatchSize = 100;
                opts.BatchTimeout = TimeSpan.FromMilliseconds(200);
            });
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "timeout-1" }, CancellationToken.None);

        // assert - batch should arrive via timeout with 1 message
        Assert.True(await recorder.WaitAsync(s_timeout), "Batch handler was not invoked via timeout");

        var batch = Assert.IsAssignableFrom<IMessageBatch<OrderCreated>>(Assert.Single(recorder.Batches));
        Assert.Equal(BatchCompletionMode.Time, batch.CompletionMode);
        Assert.Equal("timeout-1", batch[0].OrderId);
    }

    [Fact]
    public async Task Handler_Should_ReceiveMultiMessageBatch_When_ConcurrentDelivery()
    {
        // arrange - MaxBatchSize=5 with MaxConcurrency=5 so all 5 pipelines call Add()
        // concurrently, filling the batch by size before any handler completes
        var recorder = new BatchMessageRecorder();
        const int messageCount = 5;
        await using var provider = await InMemoryBusFixture.CreateBusWithTransportAsync(
            b =>
            {
                b.Services.AddSingleton(recorder);
                b.AddBatchHandler<TestBatchHandler>(opts => opts.MaxBatchSize = messageCount);
            },
            t => t.Endpoint("batch-ep").Handler<TestBatchHandler>().MaxConcurrency(messageCount));

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        for (var i = 0; i < messageCount; i++)
        {
            await bus.PublishAsync(new OrderCreated { OrderId = $"batch-{i}" }, CancellationToken.None);
        }

        // assert - single batch containing all 5 messages
        Assert.True(await recorder.WaitAsync(s_timeout), "Batch handler was not invoked within timeout");

        var batch = Assert.IsAssignableFrom<IMessageBatch<OrderCreated>>(Assert.Single(recorder.Batches));
        Assert.Equal(messageCount, batch.Count);
        Assert.Equal(BatchCompletionMode.Size, batch.CompletionMode);
    }

    [Fact]
    public async Task BatchHandler_Should_UseFreshScopeAndFeatures_When_Retried()
    {
        // arrange
        var capture = new BatchRetryCapture();
        await using var provider = await InMemoryBusFixture.CreateBusAsync(b =>
        {
            b.Services.AddSingleton(capture);
            b.Services.AddScoped<BatchScopeProbe>();
            b.AddResilience(p => p.On<Exception>()
                .Retry(1, TimeSpan.Zero, RetryBackoffType.Constant));
            b.AddBatchHandler<RetryOnceBatchHandler>(options => options.MaxBatchSize = 1);
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(
            new MutableBatchMessage { Value = "original" },
            CancellationToken.None);

        // assert
        await capture.Completed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await capture.Disposed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        new
        {
            ProbeScopeCount = capture.ProbeIds.Distinct().Count(),
            capture.RetryCounts,
            RetryFeatureCount = capture.RetryFeatures.Distinct().Count(),
            capture.RootFeatureWasPresent,
            DisposedProbeCount = capture.DisposedProbeIds.Count
        }.MatchInlineSnapshot(
            """
            {
              "ProbeScopeCount": 2,
              "RetryCounts": [
                0,
                1
              ],
              "RetryFeatureCount": 1,
              "RootFeatureWasPresent": [
                false,
                false
              ],
              "DisposedProbeCount": 2
            }
            """);
    }

    public sealed class TestBatchHandler(BatchMessageRecorder recorder) : IBatchEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(IMessageBatch<OrderCreated> batch, CancellationToken cancellationToken)
        {
            recorder.Record(batch);
            return default;
        }
    }

    public sealed class RetryOnceBatchHandler(
        BatchScopeProbe probe,
        ConsumeContextAccessor accessor,
        BatchRetryCapture capture)
        : IBatchEventHandler<MutableBatchMessage>
    {
        public ValueTask HandleAsync(
            IMessageBatch<MutableBatchMessage> batch,
            CancellationToken cancellationToken)
        {
            var context = accessor.Context!;
            capture.ProbeIds.Add(probe.Id);
            capture.RetryCounts.Add(context.Features.Get<RetryFeature>()!.ImmediateRetryCount);
            capture.RetryFeatures.Add(context.Features.Get<RetryFeature>()!);
            capture.RootFeatureWasPresent.Add(context.Features.Get<BatchAttemptFeature>() is not null);
            context.Features.Set(new BatchAttemptFeature());

            if (Interlocked.Increment(ref capture.Attempts) == 1)
            {
                throw new InvalidOperationException("Retry batch once.");
            }

            capture.Completed.TrySetResult();
            return default;
        }
    }

    public sealed class BatchScopeProbe(BatchRetryCapture capture) : IDisposable
    {
        public Guid Id { get; } = Guid.NewGuid();

        public void Dispose()
        {
            capture.DisposedProbeIds.Add(Id);

            if (capture.DisposedProbeIds.Count == 2)
            {
                capture.Disposed.TrySetResult();
            }
        }
    }

    public sealed class BatchRetryCapture
    {
        public List<Guid> ProbeIds { get; } = [];

        public List<Guid> DisposedProbeIds { get; } = [];

        public List<int> RetryCounts { get; } = [];

        public List<RetryFeature> RetryFeatures { get; } = [];

        public List<bool> RootFeatureWasPresent { get; } = [];

        public TaskCompletionSource Completed { get; } = new();

        public TaskCompletionSource Disposed { get; } = new();

        public int Attempts;
    }

    public sealed class MutableBatchMessage
    {
        public required string Value { get; set; }
    }

    public sealed class BatchAttemptFeature;
}
