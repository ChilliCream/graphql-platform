using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.Nats.Tests.Fixtures;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

public sealed record BatchedReading(string ReadingId);

// A second type for the timeout test. Sharing one type would put both tests on the same subject,
// and the second durable would replay the first test's message from the stream.
public sealed record TimedReading(string ReadingId);

// Teardown is deliberately left to the host rather than calling StopAsync: stopping disposes the
// runtime, the host then disposes it again, and every consumer is disposed twice. BatchConsumer is
// the one that notices, throwing ChannelClosedException on the second pass. That is a core defect
// rather than a transport one.
[Collection(JetStreamCollection.Name)]
public class BatchingTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task BatchHandler_Should_ReceiveOneBatchPerMessage_When_MaxBatchSizeIsOne()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new BatchMessageRecorder();

        using var host = BuildHost(recorder, options => options.MaxBatchSize = 1, "batch-size");
        await host.StartAsync(cancellationToken);

        // act
        await host.Services.GetRequiredService<IMessageBus>()
            .PublishAsync(new BatchedReading("R-1"), cancellationToken);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "The batch handler was never invoked.");

        var batch = Assert.IsAssignableFrom<IMessageBatch<BatchedReading>>(
            Assert.Single(recorder.Batches));

        Assert.Equal(BatchCompletionMode.Size, batch.CompletionMode);
        Assert.Equal([new BatchedReading("R-1")], batch.ToList());
    }

    [Fact]
    public async Task BatchHandler_Should_ReceiveTheBatch_When_TheTimeoutExpiresBeforeItFills()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new BatchMessageRecorder();

        using var host = BuildTimedHost(
            recorder,
            options =>
            {
                options.MaxBatchSize = 100;
                options.BatchTimeout = TimeSpan.FromMilliseconds(200);
            });

        await host.StartAsync(cancellationToken);

        // act
        await host.Services.GetRequiredService<IMessageBus>()
            .PublishAsync(new TimedReading("R-2"), cancellationToken);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "The batch handler was never invoked.");

        var batch = Assert.IsAssignableFrom<IMessageBatch<TimedReading>>(
            Assert.Single(recorder.Batches));

        Assert.Equal(BatchCompletionMode.Time, batch.CompletionMode);
        Assert.Equal([new TimedReading("R-2")], batch.ToList());
    }

    private IHost BuildHost(
        BatchMessageRecorder recorder,
        Action<BatchOptions> configureBatch,
        string serviceName)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddBatchHandler<BatchedReadingHandler>(configureBatch)
            .AddNats(nats => nats
                .StreamName(serviceName)
                .Endpoint($"{serviceName}-ep")
                .Handler<BatchedReadingHandler>()
                .MaxConcurrency(1));

        return builder.Build();
    }

    private IHost BuildTimedHost(BatchMessageRecorder recorder, Action<BatchOptions> configureBatch)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddBatchHandler<TimedReadingHandler>(configureBatch)
            .AddNats(nats => nats
                .StreamName("batch-timeout")
                .Endpoint("batch-timeout-ep")
                .Handler<TimedReadingHandler>()
                .MaxConcurrency(1));

        return builder.Build();
    }

    public sealed class TimedReadingHandler(BatchMessageRecorder recorder)
        : IBatchEventHandler<TimedReading>
    {
        public ValueTask HandleAsync(
            IMessageBatch<TimedReading> batch,
            CancellationToken cancellationToken)
        {
            recorder.Record(batch);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class BatchedReadingHandler(BatchMessageRecorder recorder)
        : IBatchEventHandler<BatchedReading>
    {
        public ValueTask HandleAsync(
            IMessageBatch<BatchedReading> batch,
            CancellationToken cancellationToken)
        {
            recorder.Record(batch);
            return ValueTask.CompletedTask;
        }
    }
}
