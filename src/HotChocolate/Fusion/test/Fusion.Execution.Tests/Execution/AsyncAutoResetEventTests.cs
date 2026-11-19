using System.Collections.Concurrent;

namespace HotChocolate.Fusion.Execution;

public sealed class AsyncAutoResetEventTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void IsCompleted_Should_ConsumeStoredSignal_When_SignalIsStored()
    {
        // arrange
        var signal = new AsyncAutoResetEvent();
        signal.Set();

        // act
        var first = signal.IsCompleted;
        var second = signal.IsCompleted;

        // assert
        Assert.True(first);
        Assert.False(second);
        Assert.False(signal.IsSignaled);
    }

    [Fact]
    public void IsCompleted_Should_ReturnFalse_When_NoSignalIsStored()
    {
        // arrange
        var signal = new AsyncAutoResetEvent();

        // act
        var isCompleted = signal.IsCompleted;

        // assert
        Assert.False(isCompleted);
    }

    [Fact]
    public void Await_Should_CompleteSynchronously_When_SignalIsStored()
    {
        // arrange
        var signal = new AsyncAutoResetEvent();
        signal.Set();

        // act
        var wait = WaitAsync(signal);

        // assert
        Assert.True(wait.IsCompletedSuccessfully);
        Assert.False(signal.IsSignaled);
    }

    [Fact]
    public async Task Await_Should_Suspend_When_NoSignalIsStored()
    {
        // arrange
        var signal = new AsyncAutoResetEvent();

        // act
        var wait = WaitAsync(signal);
        var completedBeforeSet = wait.IsCompleted;
        signal.Set();
        await wait.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.False(completedBeforeSet);
        Assert.False(signal.IsSignaled);
    }

    [Fact]
    public async Task UnsafeOnCompleted_Should_RunContinuation_When_SetAfterRegistration()
    {
        // arrange
        var signal = new AsyncAutoResetEvent();
        var invocations = 0;
        var continuationRan = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // act
        signal.UnsafeOnCompleted(() =>
        {
            Interlocked.Increment(ref invocations);
            continuationRan.SetResult();
        });
        var completedBeforeSet = continuationRan.Task.IsCompleted;
        signal.Set();
        await continuationRan.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.False(completedBeforeSet);
        Assert.Equal(1, invocations);
        Assert.False(signal.IsSignaled);
    }

    [Fact]
    public async Task UnsafeOnCompleted_Should_ConsumeSignalAndRunContinuation_When_SignalArrivedBeforeRegistration()
    {
        // arrange
        var signal = new AsyncAutoResetEvent();
        var continuationRan = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        signal.Set();

        // act
        signal.UnsafeOnCompleted(continuationRan.SetResult);
        await continuationRan.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.False(signal.IsSignaled);
        Assert.False(signal.TryResetToIdle());
    }

    [Fact]
    public async Task OnCompleted_Should_RunContinuationInAwaiterContext_When_SetFromAnotherContext()
    {
        // arrange
        var signal = new AsyncAutoResetEvent();
        var local = new AsyncLocal<string?>();
        var observed = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        local.Value = "awaiter";

        // act
        signal.OnCompleted(() => observed.SetResult(local.Value));
        local.Value = "setter";
        signal.Set();
        var value = await observed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("awaiter", value);
    }

    [Fact]
    public async Task UnsafeOnCompleted_Should_NotFlowSetterContext_When_SetFromAnotherContext()
    {
        // arrange
        var signal = new AsyncAutoResetEvent();
        var local = new AsyncLocal<string?>();
        var observed = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        local.Value = "awaiter";

        // act
        signal.UnsafeOnCompleted(() => observed.SetResult(local.Value));
        await Task.Run(
            () =>
            {
                local.Value = "setter";
                signal.Set();
            },
            TestContext.Current.CancellationToken);
        var value = await observed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Null(value);
    }

    [Fact]
    public async Task Await_Should_RestoreAwaiterContext_When_SetFromAnotherContext()
    {
        // arrange
        var signal = new AsyncAutoResetEvent();
        var local = new AsyncLocal<string?>();

        // act
        var wait = ObserveAfterWaitAsync(signal, local);
        local.Value = "setter";
        signal.Set();
        var value = await wait.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("awaiter", value);
    }

    [Fact]
    public async Task Set_Should_RunContinuationOnCallingThread_When_CallerIsNotThreadPoolThread()
    {
        // arrange
        var signal = new AsyncAutoResetEvent(runInlineOutsideThreadPool: true);
        var continuationThread = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var setterThread = 0;
        signal.UnsafeOnCompleted(() => continuationThread.SetResult(Environment.CurrentManagedThreadId));

        // act
        var setter = new Thread(() =>
        {
            setterThread = Environment.CurrentManagedThreadId;
            signal.Set();
        });
        setter.Start();
        setter.Join(s_timeout);
        var observedThread = await continuationThread.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(setterThread, observedThread);
        Assert.False(signal.IsSignaled);
    }

    [Fact]
    public async Task Set_Should_QueueContinuation_When_InlineRunsAreDisabled()
    {
        // arrange
        var signal = new AsyncAutoResetEvent(runInlineOutsideThreadPool: false);
        var ranOnThreadPool = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        signal.UnsafeOnCompleted(() => ranOnThreadPool.SetResult(Thread.CurrentThread.IsThreadPoolThread));

        // act
        var setter = new Thread(signal.Set);
        setter.Start();
        setter.Join(s_timeout);
        var result = await ranOnThreadPool.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void HasTwoSocketEnginesPerProcessor_Should_RequireTwiceTheProcessorCount_When_ThreadCountIsSet()
    {
        // arrange
        string?[] threadCounts = [null, "", "invalid", "-8", "4", "7", "8", "16"];

        // act
        var results = threadCounts.Select(
            threadCount => $"{threadCount ?? "null"}: "
                + AsyncAutoResetEvent.HasTwoSocketEnginesPerProcessor(threadCount, processorCount: 4));

        // assert
        results.MatchInlineSnapshots(
        [
            "null: False",
            ": False",
            "invalid: False",
            "-8: False",
            "4: False",
            "7: False",
            "8: True",
            "16: True"
        ]);
    }

    [Fact]
    public async Task Set_Should_QueueContinuation_When_CallerIsThreadPoolThread()
    {
        // arrange
        var signal = new AsyncAutoResetEvent(runInlineOutsideThreadPool: true);
        using var insideSet = new ThreadLocal<bool>();
        var ranInsideSet = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        signal.UnsafeOnCompleted(() => ranInsideSet.SetResult(insideSet.Value));

        // act
        await Task.Run(
            () =>
            {
                insideSet.Value = true;
                signal.Set();
                insideSet.Value = false;
            },
            TestContext.Current.CancellationToken);
        var result = await ranInsideSet.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.False(result);
    }

    [Fact]
    public async Task OnCompleted_Should_RunContinuationInAwaiterContext_When_SetOutsideThreadPool()
    {
        // arrange
        var signal = new AsyncAutoResetEvent(runInlineOutsideThreadPool: true);
        var local = new AsyncLocal<string?>();
        var observed = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        local.Value = "awaiter";
        signal.OnCompleted(() => observed.SetResult(local.Value));

        // act
        var setter = new Thread(() =>
        {
            local.Value = "setter";
            signal.Set();
        });
        setter.Start();
        setter.Join(s_timeout);
        var value = await observed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("awaiter", value);
    }

    [Fact]
    public void TryResetToIdle_Should_ClearStoredSignal_When_NoWaiterIsRegistered()
    {
        // arrange
        var signal = new AsyncAutoResetEvent();
        signal.Set();

        // act
        var cleared = signal.TryResetToIdle();

        // assert
        Assert.True(cleared);
        Assert.False(signal.IsCompleted);
    }

    [Fact]
    public async Task TryResetToIdle_Should_KeepWaiter_When_WaiterIsRegistered()
    {
        // arrange
        var signal = new AsyncAutoResetEvent();
        var wait = WaitAsync(signal);

        // act
        var cleared = signal.TryResetToIdle();
        signal.Set();
        await wait.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.False(cleared);
        Assert.False(signal.IsSignaled);
    }

    [Fact]
    public async Task Set_Should_WakeConsumerForEveryItem_When_ProducersRaceTheAwaiter()
    {
        // arrange
        const int iterations = 200;
        const int producers = 4;
        const int itemsPerProducer = 250;
        const int totalItems = producers * itemsPerProducer;
        var consumedPerIteration = new List<int>(iterations);

        // act
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            var signal = new AsyncAutoResetEvent();
            var queue = new ConcurrentQueue<int>();
            var consumer = ConsumeAsync(signal, queue, totalItems);

            var producerTasks = new Task[producers];

            for (var i = 0; i < producers; i++)
            {
                var yieldEvery = i + 1;
                producerTasks[i] = Task.Run(
                    async () =>
                    {
                        for (var item = 0; item < itemsPerProducer; item++)
                        {
                            queue.Enqueue(item);
                            signal.Set();

                            if (item % yieldEvery == 0)
                            {
                                await Task.Yield();
                            }
                        }
                    },
                    TestContext.Current.CancellationToken);
            }

            await Task.WhenAll(producerTasks).WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            consumedPerIteration.Add(
                await consumer.WaitAsync(s_timeout, TestContext.Current.CancellationToken));
        }

        // assert
        Assert.Equal(Enumerable.Repeat(totalItems, iterations), consumedPerIteration);

        static async Task<int> ConsumeAsync(
            AsyncAutoResetEvent signal,
            ConcurrentQueue<int> queue,
            int expectedItems)
        {
            var consumed = 0;

            while (true)
            {
                while (queue.TryDequeue(out _))
                {
                    consumed++;
                }

                if (consumed == expectedItems)
                {
                    return consumed;
                }

                await signal;
            }
        }
    }

    private static async Task WaitAsync(AsyncAutoResetEvent signal)
        => await signal;

    private static async Task<string?> ObserveAfterWaitAsync(
        AsyncAutoResetEvent signal,
        AsyncLocal<string?> local)
    {
        local.Value = "awaiter";
        await signal;
        return local.Value;
    }
}
