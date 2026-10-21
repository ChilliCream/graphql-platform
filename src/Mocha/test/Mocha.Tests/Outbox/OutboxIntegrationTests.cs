using System.Collections.Concurrent;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Outbox;

public class OutboxIntegrationTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Outbox_Should_CaptureMessage_When_EventPublished()
    {
        // arrange
        var outbox = new InMemoryMessageOutbox();
        await using var provider = await CreateBusWithOutboxAsync(outbox, _ => { });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OutboxTestEvent { Payload = "capture-me" }, CancellationToken.None);

        // assert
        await WaitUntilAsync(() => !outbox.Envelopes.IsEmpty, s_timeout);
        Assert.Single(outbox.Envelopes);
    }

    [Fact]
    public async Task Outbox_Should_CaptureMultipleMessages_When_MultipleEventsPublished()
    {
        // arrange
        var outbox = new InMemoryMessageOutbox();
        await using var provider = await CreateBusWithOutboxAsync(outbox, _ => { });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OutboxTestEvent { Payload = "first" }, CancellationToken.None);
        await bus.PublishAsync(new OutboxTestEvent { Payload = "second" }, CancellationToken.None);
        await bus.PublishAsync(new OutboxTestEvent { Payload = "third" }, CancellationToken.None);

        // assert
        await WaitUntilAsync(() => outbox.Envelopes.Count >= 3, s_timeout);
        Assert.Equal(3, outbox.Envelopes.Count);
    }

    [Fact]
    public async Task DispatchInternalAsync_Should_DeliverStoredMessage_When_OutboxIsEnabled()
    {
        // arrange
        var outbox = new InMemoryMessageOutbox();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusWithOutboxAsync(
            outbox,
            builder =>
            {
                builder.Services.AddSingleton(recorder);
                builder.AddEventHandler<OutboxTestEventHandler>();
            });
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(new OutboxTestEvent { Payload = "stored" }, CancellationToken.None);
        var envelope = Assert.Single(outbox.Envelopes);

        // act
        await DeliverAsync(scope.ServiceProvider, envelope);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout));
        new
        {
            Delivered = recorder.Messages.Cast<OutboxTestEvent>().Select(message => message.Payload),
            Stored = outbox.Envelopes.Count
        }.MatchInlineSnapshot(
            """
            {
              "Delivered": [
                "stored"
              ],
              "Stored": 1
            }
            """);
    }

    [Fact]
    public async Task Outbox_Should_SignalWorker_When_MessagePersisted()
    {
        // arrange
        var signal = new TestOutboxSignal();
        var outbox = new InMemoryMessageOutbox(signal);
        await using var provider = await CreateBusWithOutboxAsync(
            outbox,
            b =>
            {
                // Replace the default signal with our test signal
                b.Services.AddSingleton<IOutboxSignal>(signal);
            });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OutboxTestEvent { Payload = "signal-test" }, CancellationToken.None);

        // assert
        await WaitUntilAsync(() => signal.SignalCount > 0, s_timeout);
        Assert.True(signal.SignalCount >= 1, "Signal should have been set at least once");
    }

    [Fact]
    public async Task Outbox_Should_UseBatchIdAsCausation_When_PublishedFromBatchHandler()
    {
        // arrange
        var outbox = new InMemoryMessageOutbox();
        var recorder = new BatchOutboxRecorder();
        await using var provider = await CreateBusWithOutboxAsync(
            outbox,
            b =>
            {
                b.Services.AddSingleton(recorder);
                b.AddBatchHandler<OutboxBatchPublishingHandler>(opts =>
                {
                    opts.MaxBatchSize = 2;
                    opts.BatchTimeout = TimeSpan.FromSeconds(5);
                });
            },
            t => t.Endpoint("batch-ep").Handler<OutboxBatchPublishingHandler>().MaxConcurrency(2));

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var dispatcher = (IMessageDispatcher)bus;
        MessageEnvelope first = null!;
        MessageEnvelope second = null!;
        await dispatcher.PreparePublishAsync(
            new OutboxBatchInputEvent { Payload = "batch-1" },
            PublishOptions.Default,
            (envelope, _) => first = MessageEnvelope.CopyWithOwnedBody(envelope),
            CancellationToken.None);
        await dispatcher.PreparePublishAsync(
            new OutboxBatchInputEvent { Payload = "batch-2" },
            PublishOptions.Default,
            (envelope, _) => second = MessageEnvelope.CopyWithOwnedBody(envelope),
            CancellationToken.None);

        // act
        await DeliverAsync(scope.ServiceProvider, first);
        await DeliverAsync(scope.ServiceProvider, second);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Batch handler did not publish from the batch");
        await WaitUntilAsync(() => !outbox.Envelopes.IsEmpty, s_timeout);

        var envelope = Assert.Single(outbox.Envelopes);
        Assert.NotNull(recorder.BatchId);
        Assert.Equal(2, recorder.BatchCount);
        Assert.Equal(recorder.BatchId, envelope.CausationId);
    }

    private static async ValueTask DeliverAsync(IServiceProvider services, MessageEnvelope envelope)
    {
        var runtime = services.GetRequiredService<IMessagingRuntime>();
        var messageType = runtime.GetMessageType(envelope.MessageType);
        var endpoint = runtime.GetDispatchEndpoint(new Uri(envelope.DestinationAddress!));
        var pool = services.GetRequiredService<IMessagingPools>().DispatchContext;
        var context = pool.Get();
        try
        {
            context.InitializeFromEnvelope(services, endpoint, runtime, messageType, envelope, CancellationToken.None);
            await endpoint.DispatchInternalAsync(context);
        }
        finally
        {
            pool.Return(context);
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (!condition())
        {
            await Task.Delay(50, cts.Token);
        }
    }

    private static async Task<ServiceProvider> CreateBusWithOutboxAsync(
        InMemoryMessageOutbox outbox,
        Action<IMessageBusHostBuilder> configure)
        => await CreateBusWithOutboxAsync(outbox, configure, configureTransport: null);

    private static async Task<ServiceProvider> CreateBusWithOutboxAsync(
        InMemoryMessageOutbox outbox,
        Action<IMessageBusHostBuilder> configure,
        Action<IInMemoryMessagingTransportDescriptor>? configureTransport)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageOutbox>(outbox);

        var builder = services.AddMessageBus();
        builder.UseOutboxCore();

        configure(builder);
        if (configureTransport is null)
        {
            builder.AddInMemory();
        }
        else
        {
            builder.AddInMemory(configureTransport);
        }

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    public sealed class OutboxTestEvent
    {
        public required string Payload { get; init; }
    }

    public sealed class OutboxBatchInputEvent
    {
        public required string Payload { get; init; }
    }

    public sealed class OutboxTestEventHandler(MessageRecorder recorder) : IEventHandler<OutboxTestEvent>
    {
        public ValueTask HandleAsync(OutboxTestEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class OutboxBatchPublishingHandler(
        IMessageBus bus,
        ConsumeContextAccessor accessor,
        BatchOutboxRecorder recorder)
        : IBatchEventHandler<OutboxBatchInputEvent>
    {
        public async ValueTask HandleAsync(IMessageBatch<OutboxBatchInputEvent> batch, CancellationToken cancellationToken)
        {
            if (accessor.Context is not IBatchConsumeContext batchContext)
            {
                throw new InvalidOperationException("Expected an ambient batch consume context.");
            }

            await bus.PublishAsync(new OutboxTestEvent { Payload = "from-batch" }, cancellationToken);
            recorder.Record(batchContext.BatchId, batch.Count);
        }
    }

    public sealed class BatchOutboxRecorder
    {
        private readonly SemaphoreSlim _semaphore = new(0);

        public string? BatchId { get; private set; }

        public int BatchCount { get; private set; }

        public void Record(string batchId, int batchCount)
        {
            BatchId = batchId;
            BatchCount = batchCount;
            _semaphore.Release();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout)
        {
            return await _semaphore.WaitAsync(timeout);
        }
    }

    public sealed class InMemoryMessageOutbox(IOutboxSignal? signal = null) : IMessageOutbox
    {
        public ConcurrentBag<MessageEnvelope> Envelopes { get; } = [];

        public ValueTask PersistAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
        {
            Envelopes.Add(MessageEnvelope.CopyWithOwnedBody(envelope));
            signal?.Set();
            return ValueTask.CompletedTask;
        }
    }

    public sealed class TestOutboxSignal : IOutboxSignal
    {
        private int _signalCount;

        public int SignalCount => _signalCount;

        public void Set() => Interlocked.Increment(ref _signalCount);

        public Task WaitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
