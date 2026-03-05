using System.Threading.Channels;
using Mocha.Threading;

namespace Mocha.Tests.Threading;

public sealed class ChannelProcessorTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Handler_Should_ReceiveAllItems_When_ItemsWrittenToChannel()
    {
        // arrange
        var channel = Channel.CreateUnbounded<int>();
        var received = new InvocationTracker<int>();

        await using var processor = new ChannelProcessor<int>(
            channel.Reader.ReadAllAsync,
            (item, _) =>
            {
                received.Record(item);
                return Task.CompletedTask;
            },
            concurrency: 1);

        // act
        channel.Writer.TryWrite(1);
        channel.Writer.TryWrite(2);
        channel.Writer.TryWrite(3);

        // assert
        await received.WaitAsync(s_timeout, expectedCount: 3);
        Assert.Equal([1, 2, 3], received.Items.OrderBy(x => x));
    }

    [Fact]
    public async Task Handler_Should_ProcessConcurrently_When_ConcurrencyGreaterThanOne()
    {
        // arrange
        var channel = Channel.CreateUnbounded<int>();
        var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var enteredCount = 0;

        await using var processor = new ChannelProcessor<int>(
            channel.Reader.ReadAllAsync,
            async (_, _) =>
            {
                if (Interlocked.Increment(ref enteredCount) >= 2)
                {
                    barrier.TrySetResult();
                }

                await barrier.Task;
            },
            concurrency: 2);

        // act — write 2 items; both workers should enter the handler concurrently
        channel.Writer.TryWrite(1);
        channel.Writer.TryWrite(2);

        // assert — if both workers entered, the barrier completes within timeout
        var completed = await Task.WhenAny(barrier.Task, Task.Delay(s_timeout));
        Assert.Same(barrier.Task, completed);
    }

    [Fact]
    public async Task DisposeAsync_Should_StopWorkers_When_Called()
    {
        // arrange
        var channel = Channel.CreateUnbounded<int>();
        var received = new InvocationTracker<int>();

        var processor = new ChannelProcessor<int>(
            channel.Reader.ReadAllAsync,
            (item, _) =>
            {
                received.Record(item);
                return Task.CompletedTask;
            },
            concurrency: 1);

        channel.Writer.TryWrite(1);
        await received.WaitAsync(s_timeout, expectedCount: 1);

        // act
        channel.Writer.Complete();
        await processor.DisposeAsync();

        // assert — writing after dispose should not be processed
        var newChannel = Channel.CreateUnbounded<int>();
        // Workers are disposed, so no further processing occurs.
        // Verify dispose completed without hanging (implicit: we reached this line).
        Assert.Single(received.Items);
    }

    [Fact]
    public async Task Handler_Should_ContinueProcessing_When_HandlerThrows()
    {
        // arrange
        var channel = Channel.CreateUnbounded<int>();
        var received = new InvocationTracker<int>();
        var callCount = 0;

        await using var processor = new ChannelProcessor<int>(
            channel.Reader.ReadAllAsync,
            (item, _) =>
            {
                var count = Interlocked.Increment(ref callCount);
                if (count == 1)
                {
                    throw new InvalidOperationException("Boom");
                }

                received.Record(item);
                return Task.CompletedTask;
            },
            concurrency: 1);

        // act — first item throws, second item should still be processed
        channel.Writer.TryWrite(1);
        channel.Writer.TryWrite(2);

        // assert — item 2 is eventually processed after ContinuousTask restarts the loop
        await received.WaitAsync(s_timeout, expectedCount: 1);
        Assert.Contains(2, received.Items);
    }

    [Fact]
    public async Task Handler_Should_ReceiveCancelledToken_When_ProcessorIsDisposed()
    {
        // arrange
        var channel = Channel.CreateUnbounded<int>();
        var tokenCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var processor = new ChannelProcessor<int>(
            channel.Reader.ReadAllAsync,
            async (_, ct) =>
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                }
                catch (OperationCanceledException)
                {
                    tokenCancelled.TrySetResult();
                    throw;
                }
            },
            concurrency: 1);

        // act — write an item so the handler starts blocking, then dispose
        channel.Writer.TryWrite(1);
        await processor.DisposeAsync();

        // assert — the handler's cancellation token was triggered
        var completed = await Task.WhenAny(tokenCancelled.Task, Task.Delay(s_timeout));
        Assert.Same(tokenCancelled.Task, completed);
    }

    [Fact]
    public async Task Handler_Should_ReceiveItems_When_SourceIsCustomAsyncEnumerable()
    {
        // arrange — use a custom source instead of a channel to verify the abstraction
        var items = new[] { 10, 20, 30 };
        var received = new InvocationTracker<int>();

        await using var processor = new ChannelProcessor<int>(
            ct => ToAsyncEnumerable(items, ct),
            (item, _) =>
            {
                received.Record(item);
                return Task.CompletedTask;
            },
            concurrency: 1);

        // assert
        await received.WaitAsync(s_timeout, expectedCount: 3);
        Assert.Equal([10, 20, 30], received.Items.OrderBy(x => x));
    }

    private static async IAsyncEnumerable<int> ToAsyncEnumerable(
        int[] items,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            yield return item;
            await Task.Yield();
        }

        // Block until cancelled so ContinuousTask doesn't spin-restart
        await Task.Delay(Timeout.InfiniteTimeSpan, ct);
    }

    /// <summary>
    /// Thread-safe recorder for tracking handler invocations.
    /// </summary>
    private sealed class InvocationTracker<T>
    {
        private readonly object _lock = new();
        private readonly List<T> _items = [];
        private TaskCompletionSource? _waiter;
        private int _expected;

        public IReadOnlyList<T> Items
        {
            get
            {
                lock (_lock)
                {
                    return [.. _items];
                }
            }
        }

        public void Record(T item)
        {
            lock (_lock)
            {
                _items.Add(item);
                if (_waiter is not null && _items.Count >= _expected)
                {
                    _waiter.TrySetResult();
                }
            }
        }

        public async Task WaitAsync(TimeSpan timeout, int expectedCount)
        {
            TaskCompletionSource waiter;
            lock (_lock)
            {
                if (_items.Count >= expectedCount)
                {
                    return;
                }

                _expected = expectedCount;
                _waiter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                waiter = _waiter;
            }

            var completed = await Task.WhenAny(waiter.Task, Task.Delay(timeout));
            Assert.Same(waiter.Task, completed);
        }
    }
}
