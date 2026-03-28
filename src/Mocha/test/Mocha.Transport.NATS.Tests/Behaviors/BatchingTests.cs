using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.NATS.Tests.Helpers;
using NATS.Client.Core;

namespace Mocha.Transport.NATS.Tests.Behaviors;

[Collection("NATS")]
public class BatchingTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);
    private readonly NatsFixture _fixture;

    public BatchingTests(NatsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handler_Should_ReceiveBatch_When_SingleMessageSizeTrigger()
    {
        // arrange
        var recorder = new BatchMessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(new NatsConnection(_fixture.CreateOptions()))
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddBatchHandler<TestBatchHandler>(opts => opts.MaxBatchSize = 1)
            .AddNats(t => t.Endpoint("batch-ep").Handler<TestBatchHandler>().MaxConcurrency(1).MaxPrefetch(10))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "1" }, CancellationToken.None);

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
        // arrange
        var recorder = new BatchMessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(new NatsConnection(_fixture.CreateOptions()))
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddBatchHandler<TestBatchHandler>(opts =>
            {
                opts.MaxBatchSize = 100;
                opts.BatchTimeout = TimeSpan.FromMilliseconds(200);
            })
            .AddNats(t => t.Endpoint("batch-ep").Handler<TestBatchHandler>().MaxConcurrency(1).MaxPrefetch(10))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "timeout-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Batch handler was not invoked via timeout");

        var batch = Assert.IsAssignableFrom<IMessageBatch<OrderCreated>>(Assert.Single(recorder.Batches));
        Assert.Equal(BatchCompletionMode.Time, batch.CompletionMode);
        Assert.Equal("timeout-1", batch[0].OrderId);
    }

    [Fact]
    public async Task Handler_Should_ReceiveMultiMessageBatch_When_ConcurrentDelivery()
    {
        // arrange
        var recorder = new BatchMessageRecorder();
        const int messageCount = 5;
        await using var bus = await new ServiceCollection()
            .AddSingleton(new NatsConnection(_fixture.CreateOptions()))
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddBatchHandler<TestBatchHandler>(opts => opts.MaxBatchSize = messageCount)
            .AddNats(t =>
            {
                t.Endpoint("batch-ep")
                    .Handler<TestBatchHandler>()
                    .MaxConcurrency(messageCount)
                    .MaxPrefetch(messageCount);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        for (var i = 0; i < messageCount; i++)
        {
            await messageBus.PublishAsync(new OrderCreated { OrderId = $"batch-{i}" }, CancellationToken.None);
        }

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Batch handler was not invoked within timeout");

        var batch = Assert.IsAssignableFrom<IMessageBatch<OrderCreated>>(Assert.Single(recorder.Batches));
        Assert.Equal(messageCount, batch.Count);
        Assert.Equal(BatchCompletionMode.Size, batch.CompletionMode);
    }

    public sealed class TestBatchHandler(BatchMessageRecorder recorder) : IBatchEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(IMessageBatch<OrderCreated> batch, CancellationToken cancellationToken)
        {
            recorder.Record(batch);
            return default;
        }
    }
}
