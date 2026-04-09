using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Behaviors;

[Collection("AzureServiceBus")]
public class BatchingTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);
    private readonly AzureServiceBusFixture _fixture;

    public BatchingTests(AzureServiceBusFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handler_Should_ReceiveBatch_When_SingleMessageSizeTrigger()
    {
        // arrange - MaxBatchSize=1 so each message immediately triggers a batch
        var recorder = new BatchMessageRecorder();
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddBatchHandler<TestBatchHandler>(opts => opts.MaxBatchSize = 1)
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.Endpoint("batch-ep").Handler<TestBatchHandler>().MaxConcurrency(1).PrefetchCount(10);
            })
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
        // arrange - high max size so only the timer triggers dispatch
        var recorder = new BatchMessageRecorder();
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddBatchHandler<TestBatchHandler>(opts =>
            {
                opts.MaxBatchSize = 100;
                opts.BatchTimeout = TimeSpan.FromMilliseconds(200);
            })
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.Endpoint("batch-ep").Handler<TestBatchHandler>().MaxConcurrency(1).PrefetchCount(10);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "timeout-1" }, CancellationToken.None);

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
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddBatchHandler<TestBatchHandler>(opts => opts.MaxBatchSize = messageCount)
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.Endpoint("batch-ep")
                    .Handler<TestBatchHandler>()
                    .MaxConcurrency(messageCount)
                    .PrefetchCount(messageCount);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        for (var i = 0; i < messageCount; i++)
        {
            await messageBus.PublishAsync(new OrderCreated { OrderId = $"batch-{i}" }, CancellationToken.None);
        }

        // assert - single batch containing all 5 messages
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
