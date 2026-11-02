using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace HotChocolate.Fusion.Aspire;

public sealed class GatewayRecompositionWorkerTests
{
    private static readonly TimeSpan s_waitTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan s_debounceDelay = TimeSpan.FromSeconds(1);

    [Fact]
    public async Task RunAsync_Should_RecomposeOnce_When_TriggersArriveBeforeProcessingStarts()
    {
        // arrange
        var recomposeCount = 0;
        var recomposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var worker = new GatewayRecompositionWorker(
            "gateway",
            _ =>
            {
                Interlocked.Increment(ref recomposeCount);
                recomposed.TrySetResult();
                return Task.CompletedTask;
            },
            TimeSpan.Zero,
            TimeProvider.System,
            new RecordingLogger<SchemaComposition>());
        using var stopSource = new CancellationTokenSource();

        // act
        for (var i = 0; i < 5; i++)
        {
            worker.TriggerRecomposition();
        }

        var run = worker.RunAsync(stopSource.Token);
        await recomposed.Task.WaitAsync(s_waitTimeout, TestContext.Current.CancellationToken);
        stopSource.Cancel();
        await run;

        // assert
        Assert.Equal(1, recomposeCount);
    }

    [Fact]
    public async Task RunAsync_Should_CoalesceTriggers_When_RecompositionIsRunning()
    {
        // arrange
        var recomposeCount = 0;
        var firstRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstRunGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondRunCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var worker = new GatewayRecompositionWorker(
            "gateway",
            async _ =>
            {
                if (Interlocked.Increment(ref recomposeCount) == 1)
                {
                    firstRunStarted.TrySetResult();
                    await firstRunGate.Task;
                }
                else
                {
                    secondRunCompleted.TrySetResult();
                }
            },
            TimeSpan.Zero,
            TimeProvider.System,
            new RecordingLogger<SchemaComposition>());
        using var stopSource = new CancellationTokenSource();
        var run = worker.RunAsync(stopSource.Token);

        // act
        // three triggers arrive while the first recomposition is still running
        worker.TriggerRecomposition();
        await firstRunStarted.Task.WaitAsync(s_waitTimeout, TestContext.Current.CancellationToken);
        worker.TriggerRecomposition();
        worker.TriggerRecomposition();
        worker.TriggerRecomposition();
        firstRunGate.TrySetResult();
        await secondRunCompleted.Task.WaitAsync(s_waitTimeout, TestContext.Current.CancellationToken);
        stopSource.Cancel();
        await run;

        // assert
        Assert.Equal(2, recomposeCount);
    }

    [Fact]
    public async Task RunAsync_Should_KeepProcessingTriggers_When_RecompositionThrows()
    {
        // arrange
        var recomposeCount = 0;
        var firstRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondRunCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var logger = new RecordingLogger<SchemaComposition>();
        var worker = new GatewayRecompositionWorker(
            "gateway",
            _ =>
            {
                if (Interlocked.Increment(ref recomposeCount) == 1)
                {
                    firstRunStarted.TrySetResult();
                    throw new InvalidOperationException("Composition failed.");
                }

                secondRunCompleted.TrySetResult();
                return Task.CompletedTask;
            },
            TimeSpan.Zero,
            TimeProvider.System,
            logger);
        using var stopSource = new CancellationTokenSource();
        var run = worker.RunAsync(stopSource.Token);

        // act
        worker.TriggerRecomposition();
        await firstRunStarted.Task.WaitAsync(s_waitTimeout, TestContext.Current.CancellationToken);
        worker.TriggerRecomposition();
        await secondRunCompleted.Task.WaitAsync(s_waitTimeout, TestContext.Current.CancellationToken);
        stopSource.Cancel();
        await run;

        // assert
        var errorLog = string.Join(
            Environment.NewLine,
            logger.Entries
                .Where(entry => entry.Level is LogLevel.Error)
                .Select(entry =>
                    $"{entry.Message} | Exception: {entry.Exception?.Message ?? "<none>"}"));

        $$"""
        Recompositions: {{recomposeCount}}
        Errors:
        {{errorLog}}
        """.MatchInlineSnapshot(
            """
            Recompositions: 2
            Errors:
            Schema recomposition failed for gateway: Composition failed. | Exception: Composition failed.
            """);
    }

    [Fact]
    public async Task RunAsync_Should_WaitForDebounceDelay_When_DebounceDelayIsConfigured()
    {
        // arrange
        var recomposeCount = 0;
        var recomposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var timeProvider = new TimerTrackingFakeTimeProvider();
        var worker = new GatewayRecompositionWorker(
            "gateway",
            _ =>
            {
                Interlocked.Increment(ref recomposeCount);
                recomposed.TrySetResult();
                return Task.CompletedTask;
            },
            s_debounceDelay,
            timeProvider,
            new RecordingLogger<SchemaComposition>());
        using var stopSource = new CancellationTokenSource();
        var run = worker.RunAsync(stopSource.Token);

        // act
        worker.TriggerRecomposition();
        await timeProvider.TimerCreations
            .ReadAsync(TestContext.Current.CancellationToken)
            .AsTask()
            .WaitAsync(s_waitTimeout, TestContext.Current.CancellationToken);

        // fake time is frozen, so the recomposition cannot run before the delay is advanced
        var recomposedDuringDebounce = recomposeCount;
        timeProvider.Advance(s_debounceDelay);
        await recomposed.Task.WaitAsync(s_waitTimeout, TestContext.Current.CancellationToken);
        stopSource.Cancel();
        await run;

        // assert
        Assert.Equal(0, recomposedDuringDebounce);
        Assert.Equal(1, recomposeCount);
    }

    [Fact]
    public async Task RunAsync_Should_CoalesceTriggers_When_TriggersArriveDuringDebounceWindow()
    {
        // arrange
        var recomposeCount = 0;
        var recomposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var timeProvider = new TimerTrackingFakeTimeProvider();
        var worker = new GatewayRecompositionWorker(
            "gateway",
            _ =>
            {
                Interlocked.Increment(ref recomposeCount);
                recomposed.TrySetResult();
                return Task.CompletedTask;
            },
            s_debounceDelay,
            timeProvider,
            new RecordingLogger<SchemaComposition>());
        using var stopSource = new CancellationTokenSource();
        var run = worker.RunAsync(stopSource.Token);

        // act
        // two more triggers arrive while the debounce window of the first trigger is open
        worker.TriggerRecomposition();
        await timeProvider.TimerCreations
            .ReadAsync(TestContext.Current.CancellationToken)
            .AsTask()
            .WaitAsync(s_waitTimeout, TestContext.Current.CancellationToken);
        worker.TriggerRecomposition();
        worker.TriggerRecomposition();
        timeProvider.Advance(s_debounceDelay);
        await recomposed.Task.WaitAsync(s_waitTimeout, TestContext.Current.CancellationToken);

        // a leftover trigger would open a second debounce window shortly after the first run;
        // the settle delay can only miss a regression, it cannot fail a correct worker
        await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
        timeProvider.Advance(s_debounceDelay);
        stopSource.Cancel();
        await run;

        // assert
        Assert.False(timeProvider.TimerCreations.TryRead(out _));
        Assert.Equal(1, recomposeCount);
    }

    private sealed class TimerTrackingFakeTimeProvider : FakeTimeProvider
    {
        private readonly Channel<TimeSpan> _timerCreations = Channel.CreateUnbounded<TimeSpan>();

        public ChannelReader<TimeSpan> TimerCreations => _timerCreations.Reader;

        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period)
        {
            var timer = base.CreateTimer(callback, state, dueTime, period);
            _timerCreations.Writer.TryWrite(dueTime);
            return timer;
        }
    }
}
