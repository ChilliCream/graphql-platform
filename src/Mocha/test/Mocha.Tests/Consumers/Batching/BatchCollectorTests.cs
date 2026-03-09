using Microsoft.Extensions.Time.Testing;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Tests.Consumers.Batching;

public sealed class BatchCollectorTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Add_Should_DispatchBatch_When_MaxBatchSizeReached()
    {
        // arrange
        var dispatched = new BatchRecorder<TestEvent>();
        await using var collector = CreateCollector(dispatched, opts => opts.MaxBatchSize = 3);

        // act
        await AddEntries(collector, 3);

        // assert
        Assert.True(await dispatched.WaitAsync(s_timeout), "Batch was not dispatched when MaxBatchSize reached");

        var batch = dispatched.Single();
        Assert.Equal(3, batch.Count);
        Assert.Equal(BatchCompletionMode.Size, batch.CompletionMode);
    }

    [Fact]
    public async Task Add_Should_DispatchBatch_When_TimerFires()
    {
        // arrange
        var fakeTime = new FakeTimeProvider();
        var dispatched = new BatchRecorder<TestEvent>();
        var timeout = TimeSpan.FromSeconds(2);
        await using var collector = CreateCollector(
            dispatched,
            opts =>
            {
                opts.MaxBatchSize = 100;
                opts.BatchTimeout = timeout;
            },
            timeProvider: fakeTime);

        // act — add 1 message (below max size), advance time past timeout
        await AddEntries(collector, 1);
        fakeTime.Advance(timeout.Add(TimeSpan.FromMilliseconds(10)));

        // assert
        Assert.True(await dispatched.WaitAsync(s_timeout), "Batch was not dispatched when timer fired");

        var batch = dispatched.Single();
        Assert.Single(batch);
        Assert.Equal(BatchCompletionMode.Time, batch.CompletionMode);
    }

    [Fact]
    public async Task DisposeAsync_Should_FlushRemaining_When_BufferHasMessages()
    {
        // arrange
        var dispatched = new BatchRecorder<TestEvent>();
        var collector = CreateCollector(dispatched, opts => opts.MaxBatchSize = 100);

        await AddEntries(collector, 5);

        // act
        await collector.DisposeAsync();

        // assert
        Assert.True(await dispatched.WaitAsync(s_timeout), "Remaining buffer was not flushed on dispose");

        var batch = dispatched.Single();
        Assert.Equal(5, batch.Count);
        Assert.Equal(BatchCompletionMode.Forced, batch.CompletionMode);
    }

    [Fact]
    public async Task Add_Should_Throw_When_CollectorDisposed()
    {
        // arrange
        var dispatched = new BatchRecorder<TestEvent>();
        var collector = CreateCollector(dispatched, opts => opts.MaxBatchSize = 100);

        await collector.DisposeAsync();

        // act & assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => collector.Add(CreateContext("x")).AsTask());
    }

    [Fact]
    public async Task Add_Should_DispatchMultipleBatches_When_MoreThanMaxSizeAdded()
    {
        // arrange
        var dispatched = new BatchRecorder<TestEvent>();
        await using var collector = CreateCollector(dispatched, opts => opts.MaxBatchSize = 3);

        // act — add 7 entries: expect 2 full batches (3 + 3) and 1 remaining
        await AddEntries(collector, 7);

        // wait for the two size-triggered batches
        Assert.True(await dispatched.WaitAsync(s_timeout, expectedCount: 2));

        // dispose to flush the remaining 1
        await collector.DisposeAsync();

        // wait for 1 more batch (the forced flush)
        Assert.True(await dispatched.WaitAsync(s_timeout, expectedCount: 1));

        // assert
        Assert.Equal(3, dispatched.Batches.Count);

        var sizes = dispatched.Batches.Select(b => b.Count).OrderByDescending(s => s).ToList();
        Assert.Equal([3, 3, 1], sizes);
    }

    [Fact]
    public async Task Add_Should_BeThreadSafe_When_CalledConcurrently()
    {
        // arrange
        var dispatched = new BatchRecorder<TestEvent>();
        const int batchSize = 10;
        const int totalMessages = 100;
        await using var collector = CreateCollector(dispatched, opts => opts.MaxBatchSize = batchSize);

        // act — add messages concurrently from multiple threads
        var tasks = Enumerable
            .Range(0, totalMessages)
            .Select(i => Task.Run(async () => await collector.Add(CreateContext($"msg-{i}"))))
            .ToArray();

        await Task.WhenAll(tasks);

        // wait for all full batches (100/10 = 10 batches)
        Assert.True(
            await dispatched.WaitAsync(s_timeout, expectedCount: totalMessages / batchSize),
            "Not all batches were dispatched under concurrent load");

        // assert — total messages across all batches should equal totalMessages
        var totalDispatched = dispatched.Batches.Sum(b => b.Count);
        Assert.Equal(totalMessages, totalDispatched);
    }

    [Fact]
    public async Task Add_Should_DispatchTwoFullBatches_When_ConcurrentProducersSendDoubleMaxSize()
    {
        // arrange
        var dispatched = new BatchRecorder<TestEvent>();
        const int batchSize = 5;
        const int totalMessages = batchSize * 2;
        await using var collector = CreateCollector(dispatched, opts => opts.MaxBatchSize = batchSize);

        // act — add 2×MaxBatchSize messages concurrently
        var tasks = Enumerable
            .Range(0, totalMessages)
            .Select(_ => Task.Run(async () => await collector.Add(CreateContext("x"))))
            .ToArray();

        await Task.WhenAll(tasks);

        // assert — exactly 2 batches dispatched
        Assert.True(
            await dispatched.WaitAsync(s_timeout, expectedCount: 2),
            "Expected exactly 2 batches to be dispatched");

        Assert.Equal(2, dispatched.Batches.Count);
        Assert.All(dispatched.Batches, b => Assert.Equal(batchSize, b.Count));

        // verify all entries dispatched
        var totalDispatched = dispatched.Batches.Sum(b => b.Count);
        Assert.Equal(totalMessages, totalDispatched);
    }

    [Fact]
    public async Task Add_Should_PreserveOrdering_When_MessagesAddedSequentially()
    {
        // arrange
        var dispatched = new BatchRecorder<TestEvent>();
        await using var collector = CreateCollector(dispatched, opts => opts.MaxBatchSize = 5);

        // act — add 5 messages sequentially
        for (var i = 0; i < 5; i++)
        {
            await collector.Add(CreateContext($"msg-{i}"));
        }

        // assert — batch dispatched in order
        Assert.True(await dispatched.WaitAsync(s_timeout), "Batch was not dispatched");

        var batch = dispatched.Single();
        Assert.Equal(5, batch.Count);

        for (var i = 0; i < 5; i++)
        {
            Assert.Equal($"msg-{i}", batch[i].Id);
        }
    }

    private static BatchCollector<TestEvent> CreateCollector(
        BatchRecorder<TestEvent> recorder,
        Action<BatchOptions>? configure = null,
        TimeProvider? timeProvider = null)
    {
        return CreateCollector(
            onBatch: batch =>
            {
                recorder.Record(batch);
                return ValueTask.CompletedTask;
            },
            configure,
            timeProvider);
    }

    private static BatchCollector<TestEvent> CreateCollector(
        Func<MessageBatch<TestEvent>, ValueTask> onBatch,
        Action<BatchOptions>? configure = null,
        TimeProvider? timeProvider = null)
    {
        var options = new BatchOptions();
        configure?.Invoke(options);

        return new BatchCollector<TestEvent>(options, onBatch, timeProvider ?? TimeProvider.System);
    }

    private static async Task AddEntries(BatchCollector<TestEvent> collector, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await collector.Add(CreateContext($"msg-{i}"));
        }
    }

    private static StubConsumeContext CreateContext(string id) => new(new TestEvent { Id = id }, id);

    public sealed class TestEvent
    {
        public required string Id { get; init; }
    }

    private sealed class StubConsumeContext(TestEvent message, string? messageId = null) : IConsumeContext<TestEvent>
    {
        public TestEvent Message => message;
        public IFeatureCollection Features { get; } = new FeatureCollection();
        public IReadOnlyHeaders Headers { get; } = new Headers();
        public MessagingTransport Transport { get; set; } = null!;
        public ReceiveEndpoint Endpoint { get; set; } = null!;
        public string? MessageId { get; set; } = messageId;
        public string? CorrelationId { get; set; }
        public string? ConversationId { get; set; }
        public string? CausationId { get; set; }
        public Uri? SourceAddress { get; set; }
        public Uri? DestinationAddress { get; set; }
        public Uri? ResponseAddress { get; set; }
        public Uri? FaultAddress { get; set; }
        public MessageContentType? ContentType { get; set; }
        public MessageType? MessageType { get; set; }
        public DateTimeOffset? SentAt { get; set; }
        public DateTimeOffset? DeliverBy { get; set; }
        public int? DeliveryCount { get; set; }
        public ReadOnlyMemory<byte> Body => ReadOnlyMemory<byte>.Empty;
        public MessageEnvelope? Envelope { get; set; }
        public IRemoteHostInfo Host { get; set; } = null!;
        public IMessagingRuntime Runtime { get; set; } = null!;
        public CancellationToken CancellationToken { get; set; }
        public IServiceProvider Services { get; set; } = null!;
    }

    /// <summary>
    /// Thread-safe recorder for batch dispatches.
    /// </summary>
    private sealed class BatchRecorder<TEvent>
    {
        private readonly SemaphoreSlim _semaphore = new(0);
        private readonly object _sync = new();
        private readonly List<MessageBatch<TEvent>> _batches = [];

        public IReadOnlyList<MessageBatch<TEvent>> Batches
        {
            get
            {
                lock (_sync)
                {
                    return _batches.ToList();
                }
            }
        }

        public void Record(MessageBatch<TEvent> batch)
        {
            lock (_sync)
            {
                _batches.Add(batch);
            }

            _semaphore.Release();
        }

        public MessageBatch<TEvent> Single()
        {
            lock (_sync)
            {
                return Assert.Single(_batches);
            }
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount = 1)
        {
            for (var i = 0; i < expectedCount; i++)
            {
                if (!await _semaphore.WaitAsync(timeout))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
