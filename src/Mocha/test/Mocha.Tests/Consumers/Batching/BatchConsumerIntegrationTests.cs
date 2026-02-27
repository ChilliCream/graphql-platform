using Microsoft.Extensions.DependencyInjection;
using Mocha.Tests.IntegrationTests;

namespace Mocha.Tests.Consumers.Batching;

/// <summary>
/// Integration tests for BatchConsumer using the InMemory transport.
/// </summary>
public sealed class BatchConsumerIntegrationTests : ConsumerIntegrationTestsBase
{
    [Fact]
    public async Task Handler_Should_ReceiveBatch_When_SingleMessageSizeTrigger()
    {
        // arrange — MaxBatchSize=1 so each message immediately triggers a batch
        var recorder = new BatchMessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddBatchHandler<TestBatchHandler>(opts => opts.MaxBatchSize = 1);
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TestBatchEvent { Id = "1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout), "Batch handler was not invoked within timeout");

        var batch = Assert.IsAssignableFrom<IMessageBatch<TestBatchEvent>>(Assert.Single(recorder.Batches));
        Assert.Single(batch);
        Assert.Equal(BatchCompletionMode.Size, batch.CompletionMode);
        Assert.Equal("1", batch[0].Id);
    }

    [Fact]
    public async Task Handler_Should_ReceiveBatch_When_TimeoutExpires()
    {
        // arrange — high max size so only the timer triggers dispatch
        var recorder = new BatchMessageRecorder();
        await using var provider = await CreateBusAsync(b =>
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
        await bus.PublishAsync(new TestBatchEvent { Id = "timeout-1" }, CancellationToken.None);

        // assert — batch should arrive via timeout with 1 message
        Assert.True(await recorder.WaitAsync(Timeout), "Batch handler was not invoked via timeout");

        var batch = Assert.IsAssignableFrom<IMessageBatch<TestBatchEvent>>(Assert.Single(recorder.Batches));
        Assert.Equal(BatchCompletionMode.Time, batch.CompletionMode);
        Assert.Equal("timeout-1", batch[0].Id);
    }

    [Fact]
    public async Task Handler_Should_ReceiveMultipleBatches_When_MultipleMessagesPublished()
    {
        // arrange — MaxBatchSize=1 so each message triggers its own batch
        var recorder = new BatchMessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddBatchHandler<TestBatchHandler>(opts => opts.MaxBatchSize = 1);
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        for (var i = 0; i < 3; i++)
        {
            await bus.PublishAsync(new TestBatchEvent { Id = $"multi-{i}" }, CancellationToken.None);
        }

        // assert
        Assert.True(await recorder.WaitAsync(Timeout, expectedCount: 3), "Did not receive 3 batches within timeout");

        Assert.Equal(3, recorder.Batches.Count);
        var totalMessages = recorder.Batches.Cast<IMessageBatch<TestBatchEvent>>().Sum(b => b.Count);
        Assert.Equal(3, totalMessages);
    }

    [Fact]
    public async Task Handler_Should_ReceiveMetadata_When_BatchDelivered()
    {
        // arrange
        var recorder = new BatchMessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddBatchHandler<TestBatchHandler>(opts => opts.MaxBatchSize = 1);
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TestBatchEvent { Id = "meta-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout));
        var batch = Assert.IsAssignableFrom<IMessageBatch<TestBatchEvent>>(Assert.Single(recorder.Batches));

        // Message is eagerly captured and survives context recycling
        Assert.Equal("meta-1", batch[0].Id);
    }

    [Fact]
    public async Task Handler_Should_ResolveDependencies_When_InvokedThroughDI()
    {
        // arrange
        var recorder = new BatchMessageRecorder();
        var counter = new InvocationCounter();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.Services.AddSingleton(counter);
            b.AddBatchHandler<CountingBatchHandler>(opts => opts.MaxBatchSize = 1);
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TestBatchEvent { Id = "di-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout));
        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task Handler_Should_NotCrashRuntime_When_ExceptionThrown()
    {
        // arrange — publish to a throwing handler, then verify a normal handler still works
        var throwingRecorder = new BatchMessageRecorder();
        var normalRecorder = new BatchMessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddKeyedSingleton("throwing", throwingRecorder);
            b.Services.AddKeyedSingleton("normal", normalRecorder);
            b.AddBatchHandler<ThrowingBatchHandler>(opts => opts.MaxBatchSize = 1);
            b.AddBatchHandler<NormalOtherBatchHandler>(opts => opts.MaxBatchSize = 1);
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act — publish event that triggers a throw in the batch handler
        await bus.PublishAsync(new TestBatchEvent { Id = "fail" }, CancellationToken.None);

        // wait briefly for the throwing handler to process
        await throwingRecorder.WaitAsync(TimeSpan.FromSeconds(2));

        // publish a different event type to the normal handler
        await bus.PublishAsync(new OtherBatchEvent { Name = "ok" }, CancellationToken.None);

        // assert — the normal handler still works
        Assert.True(
            await normalRecorder.WaitAsync(Timeout),
            "Normal handler did not receive event after a previous handler threw");
    }

    [Fact]
    public async Task Handler_Should_ProcessBothHandlers_When_TwoBatchHandlersForSameEvent()
    {
        // arrange — two different batch handlers for the same event type
        var recorder1 = new BatchMessageRecorder();
        var recorder2 = new BatchMessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddKeyedSingleton("handler1", recorder1);
            b.Services.AddKeyedSingleton("handler2", recorder2);
            b.AddBatchHandler<TestBatchHandlerA>(opts => opts.MaxBatchSize = 1);
            b.AddBatchHandler<TestBatchHandlerB>(opts => opts.MaxBatchSize = 1);
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TestBatchEvent { Id = "dual-1" }, CancellationToken.None);

        // assert — both handlers receive the event independently
        Assert.True(await recorder1.WaitAsync(Timeout), "First batch handler did not receive the event");
        Assert.True(await recorder2.WaitAsync(Timeout), "Second batch handler did not receive the event");

        var batch1 = Assert.IsAssignableFrom<IMessageBatch<TestBatchEvent>>(Assert.Single(recorder1.Batches));
        var batch2 = Assert.IsAssignableFrom<IMessageBatch<TestBatchEvent>>(Assert.Single(recorder2.Batches));
        Assert.Equal("dual-1", batch1[0].Id);
        Assert.Equal("dual-1", batch2[0].Id);
    }

    [Fact]
    public async Task Handler_Should_ProcessBothHandlerTypes_When_BatchAndRegularHandlerCoexist()
    {
        // arrange — both IBatchEventHandler<T> and IEventHandler<T> for the same event type
        var batchRecorder = new BatchMessageRecorder();
        var eventRecorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(batchRecorder);
            b.Services.AddSingleton(eventRecorder);
            b.AddBatchHandler<TestBatchHandler>(opts => opts.MaxBatchSize = 1);
            b.AddEventHandler<TestEventHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TestBatchEvent { Id = "both-1" }, CancellationToken.None);

        // assert — both the batch handler and regular event handler receive the message
        Assert.True(await batchRecorder.WaitAsync(Timeout), "Batch handler did not receive the event");
        Assert.True(await eventRecorder.WaitAsync(Timeout), "Regular event handler did not receive the event");

        var batch = Assert.IsAssignableFrom<IMessageBatch<TestBatchEvent>>(Assert.Single(batchRecorder.Batches));
        Assert.Equal("both-1", batch[0].Id);

        var eventMsg = Assert.Single(eventRecorder.Messages);
        Assert.Equal("both-1", ((TestBatchEvent)eventMsg).Id);
    }

    [Fact]
    public async Task Handler_Should_ReceiveMultiMessageBatch_When_ConcurrentDelivery()
    {
        // arrange — MaxBatchSize=5 with MaxConcurrency=5 so all 5 pipelines call Add()
        // concurrently, filling the batch by size before any handler completes
        var recorder = new BatchMessageRecorder();
        const int messageCount = 5;
        await using var provider = await CreateBusAsync(
            b =>
            {
                b.Services.AddSingleton(recorder);
                b.AddBatchHandler<TestBatchHandler>(opts => opts.MaxBatchSize = messageCount);
            },
            t =>
                t.Endpoint("batch-ep").Handler<TestBatchHandler>().MaxConcurrency(messageCount));

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var publishTasks = Enumerable.Range(0, messageCount)
            .Select(i => bus.PublishAsync(new TestBatchEvent { Id = $"batch-{i}" }, CancellationToken.None).AsTask());
        await Task.WhenAll(publishTasks);

        // assert — single batch containing all 5 messages
        Assert.True(await recorder.WaitAsync(Timeout), "Batch handler was not invoked within timeout");

        var batch = Assert.IsAssignableFrom<IMessageBatch<TestBatchEvent>>(Assert.Single(recorder.Batches));
        Assert.Equal(messageCount, batch.Count);
        Assert.Equal(BatchCompletionMode.Size, batch.CompletionMode);
    }

    // --- Test types ---

    public sealed class TestBatchEvent
    {
        public required string Id { get; init; }
    }

    public sealed class OtherBatchEvent
    {
        public required string Name { get; init; }
    }

    public sealed class TestBatchHandler(BatchMessageRecorder recorder) : IBatchEventHandler<TestBatchEvent>
    {
        public ValueTask HandleAsync(IMessageBatch<TestBatchEvent> batch, CancellationToken cancellationToken)
        {
            recorder.Record(batch);
            return default;
        }
    }

    public sealed class TestBatchHandlerA([FromKeyedServices("handler1")] BatchMessageRecorder recorder)
        : IBatchEventHandler<TestBatchEvent>
    {
        public ValueTask HandleAsync(IMessageBatch<TestBatchEvent> batch, CancellationToken cancellationToken)
        {
            recorder.Record(batch);
            return default;
        }
    }

    public sealed class TestBatchHandlerB([FromKeyedServices("handler2")] BatchMessageRecorder recorder)
        : IBatchEventHandler<TestBatchEvent>
    {
        public ValueTask HandleAsync(IMessageBatch<TestBatchEvent> batch, CancellationToken cancellationToken)
        {
            recorder.Record(batch);
            return default;
        }
    }

    public sealed class TestEventHandler(MessageRecorder recorder) : IEventHandler<TestBatchEvent>
    {
        public ValueTask HandleAsync(TestBatchEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class CountingBatchHandler(BatchMessageRecorder recorder, InvocationCounter counter)
        : IBatchEventHandler<TestBatchEvent>
    {
        public ValueTask HandleAsync(IMessageBatch<TestBatchEvent> batch, CancellationToken cancellationToken)
        {
            counter.Increment();
            recorder.Record(batch);
            return default;
        }
    }

    public sealed class ThrowingBatchHandler([FromKeyedServices("throwing")] BatchMessageRecorder recorder)
        : IBatchEventHandler<TestBatchEvent>
    {
        public ValueTask HandleAsync(IMessageBatch<TestBatchEvent> batch, CancellationToken cancellationToken)
        {
            recorder.Record(batch);
            throw new InvalidOperationException("Batch handler failed deliberately");
        }
    }

    public sealed class NormalOtherBatchHandler([FromKeyedServices("normal")] BatchMessageRecorder recorder)
        : IBatchEventHandler<OtherBatchEvent>
    {
        public ValueTask HandleAsync(IMessageBatch<OtherBatchEvent> batch, CancellationToken cancellationToken)
        {
            recorder.Record(batch);
            return default;
        }
    }
}
