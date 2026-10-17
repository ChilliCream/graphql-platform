namespace HotChocolate.Fusion.Execution;

public sealed class AsyncAutoResetEventTests
{
    [Theory]
    [InlineData(false, "signal")]
    [InlineData(true, "registration")]
    public async Task OnCompleted_Should_FlowSchedulingContext_When_SignalOrderChanges(
        bool signalFirst,
        string expected)
    {
        // arrange
        var signal = new AsyncAutoResetEvent();
        var local = new AsyncLocal<string?>();
        var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        // act
        local.Value = "signal";
        if (signalFirst)
        {
            signal.Set();
        }

        local.Value = "registration";
        signal.OnCompleted(() => completion.SetResult(local.Value));
        if (!signalFirst)
        {
            local.Value = "signal";
            signal.Set();
        }

        var actual = await completion.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OnCompleted_Should_RespectSuppressedContext_When_Dispatching(bool signalFirst)
    {
        // arrange
        var signal = new AsyncAutoResetEvent();
        var local = new AsyncLocal<string?> { Value = "caller" };
        var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        // act
        if (signalFirst)
        {
            signal.Set();
            using (ExecutionContext.SuppressFlow())
            {
                signal.OnCompleted(() => completion.SetResult(local.Value));
            }
        }
        else
        {
            signal.OnCompleted(() => completion.SetResult(local.Value));
            using (ExecutionContext.SuppressFlow())
            {
                signal.Set();
            }
        }

        var actual = await completion.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        Assert.Null(actual);
    }

    [Fact]
    public async Task Set_Should_CoalesceSignals_When_NoAwaiterIsRegistered()
    {
        // arrange
        var signal = new AsyncAutoResetEvent();
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // act
        signal.Set();
        signal.Set();
        signal.Set();
        signal.OnCompleted(first.SetResult);
        await first.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        signal.OnCompleted(second.SetResult);
        var completedBeforeNextSignal = second.Task.IsCompleted;
        signal.Set();
        await second.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        Assert.False(completedBeforeNextSignal);
        Assert.False(signal.IsSignaled);
        Assert.Equal(TaskStatus.RanToCompletion, second.Task.Status);
    }

    [Fact]
    public async Task OnCompleted_Should_AllowReentrantAwaiters_When_ContinuationSchedulesNextWait()
    {
        // arrange
        var signal = new AsyncAutoResetEvent();
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        Action? continuation = null;
        continuation = () =>
        {
            if (++count == 1000)
            {
                completion.SetResult(count);
                return;
            }

            signal.OnCompleted(continuation!);
            signal.Set();
        };

        // act
        signal.OnCompleted(continuation);
        signal.Set();
        var actual = await completion.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1000, actual);
        Assert.False(signal.IsSignaled);
    }

    [Fact]
    public async Task Set_Should_CompleteEveryWait_When_ProducerAndConsumerRace()
    {
        // arrange
        var signal = new AsyncAutoResetEvent();
        var acknowledgment = new AsyncAutoResetEvent();
        var received = 0;
        var producer = Task.Run(async () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                signal.Set();
                await acknowledgment;
            }
        }, TestContext.Current.CancellationToken);

        // act
        var consumer = Task.Run(async () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                await signal;
                received++;
                acknowledgment.Set();
            }
        }, TestContext.Current.CancellationToken);
        await Task.WhenAll(producer, consumer).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1000, received);
        Assert.False(signal.IsSignaled);
        Assert.False(acknowledgment.IsSignaled);
    }

    [Fact]
    public async Task TryResetToIdle_Should_ClearStoredSignal_When_EventIsReused()
    {
        // arrange
        var signal = new AsyncAutoResetEvent();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        signal.Set();

        // act
        var reset = signal.TryResetToIdle();
        signal.OnCompleted(completion.SetResult);
        var completedBeforeNextSignal = completion.Task.IsCompleted;
        signal.Set();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        Assert.True(reset);
        Assert.False(completedBeforeNextSignal);
        Assert.False(signal.IsSignaled);
    }
}
