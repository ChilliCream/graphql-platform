using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public class BatchingTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);
    private readonly RabbitMQFixture _fixture;

    public BatchingTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handler_Should_ReceiveBatch_When_SingleMessageSizeTrigger()
    {
        // arrange — MaxBatchSize=1 so each message immediately triggers a batch
        var recorder = new BatchMessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddBatchHandler<TestBatchHandler>(opts => opts.MaxBatchSize = 1)
            .AddRabbitMQ(t => t.Endpoint("batch-ep").Handler<TestBatchHandler>().MaxConcurrency(1).MaxPrefetch(10))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout), "Batch handler was not invoked within timeout");

        var batch = Assert.IsAssignableFrom<IMessageBatch<OrderCreated>>(Assert.Single(recorder.Batches));
        Assert.Single(batch);
        Assert.Equal(BatchCompletionMode.Size, batch.CompletionMode);
        Assert.Equal("1", batch[0].OrderId);
    }

    [Fact]
    public async Task Handler_Should_ReceiveBatch_When_TimeoutExpires()
    {
        // arrange — high max size so only the timer triggers dispatch
        var recorder = new BatchMessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddBatchHandler<TestBatchHandler>(opts =>
            {
                opts.MaxBatchSize = 100;
                opts.BatchTimeout = TimeSpan.FromMilliseconds(200);
            })
            .AddRabbitMQ(t => t.Endpoint("batch-ep").Handler<TestBatchHandler>().MaxConcurrency(1).MaxPrefetch(10))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "timeout-1" }, CancellationToken.None);

        // assert — batch should arrive via timeout with 1 message
        Assert.True(await recorder.WaitAsync(Timeout), "Batch handler was not invoked via timeout");

        var batch = Assert.IsAssignableFrom<IMessageBatch<OrderCreated>>(Assert.Single(recorder.Batches));
        Assert.Equal(BatchCompletionMode.Time, batch.CompletionMode);
        Assert.Equal("timeout-1", batch[0].OrderId);
    }

    [Fact]
    public async Task Handler_Should_ReceiveMultiMessageBatch_When_ConcurrentDelivery()
    {
        // arrange — MaxBatchSize=5 with MaxConcurrency=5 so all 5 pipelines call Add()
        // concurrently, filling the batch by size before any handler completes
        var recorder = new BatchMessageRecorder();
        const int messageCount = 5;
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddBatchHandler<TestBatchHandler>(opts =>
            {
                opts.MaxBatchSize = messageCount;
                opts.BatchTimeout = TimeSpan.FromSeconds(60);
            })
            .AddRabbitMQ(t =>
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
        var publishTasks = Enumerable.Range(0, messageCount)
            .Select(i => messageBus.PublishAsync(new OrderCreated { OrderId = $"batch-{i}" }, CancellationToken.None)
                .AsTask());
        await Task.WhenAll(publishTasks);

        // assert — wait until all messages are observed across batch deliveries
        var deadline = DateTime.UtcNow + Timeout;
        IMessageBatch<OrderCreated>[] batches = [];

        while (DateTime.UtcNow < deadline)
        {
            batches = recorder.Batches.OfType<IMessageBatch<OrderCreated>>().ToArray();
            if (batches.Sum(t => t.Count) >= messageCount)
            {
                break;
            }

            await Task.Delay(50);
        }

        Assert.Equal(messageCount, batches.Sum(t => t.Count));
        Assert.Contains(batches, b => b.CompletionMode == BatchCompletionMode.Size);
        Assert.Contains(batches, b => b.Count > 1);
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
