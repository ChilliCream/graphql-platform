using System.Diagnostics;
using System.Threading.Channels;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Runtime;

public sealed class TuiEffectQueueTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task TrySubmit_Should_ReturnImmediately_When_EffectIsSlow()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();
        var release = new TaskCompletionSource();
        var stopwatch = Stopwatch.StartNew();

        async Task<string> SlowEffect(TuiOperationId id, CancellationToken ct)
        {
            await release.Task.WaitAsync(TestTimeout, ct);
            return "done";
        }

        // act
        var submitted = queue.TrySubmit("slot", SlowEffect, testToken, out var operationId);
        var elapsed = stopwatch.Elapsed;

        // assert
        Assert.True(submitted);
        Assert.NotEqual(default, operationId);
        Assert.True(elapsed < TimeSpan.FromMilliseconds(500), $"TrySubmit blocked for {elapsed}.");
        Assert.Equal(1, queue.PendingCount);

        release.SetResult();
        await WaitUntilAsync(() => queue.PendingCount == 0, testToken);
    }

    [Fact]
    public async Task TrySubmit_Should_ReturnFalse_When_DedupeKeyAlreadyInFlight()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();
        var release = new TaskCompletionSource();

        async Task<string> Effect(TuiOperationId id, CancellationToken ct)
        {
            await release.Task.WaitAsync(TestTimeout, ct);
            return "done";
        }

        queue.TrySubmit("compose", Effect, testToken, out var firstId);

        // act
        var duplicateSubmitted = queue.TrySubmit("compose", Effect, testToken, out var secondId);

        // assert
        Assert.False(duplicateSubmitted);
        Assert.Equal(default, secondId);
        Assert.NotEqual(default, firstId);

        release.SetResult();
        await WaitUntilAsync(() => queue.PendingCount == 0, testToken);
    }

    [Fact]
    public async Task TrySubmit_Should_ReturnImmediately_When_EffectBlocksSynchronouslyBeforeFirstAwait()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();
        using var gate = new ManualResetEventSlim(false);
        var stopwatch = Stopwatch.StartNew();

        Task<string> BlockingEffect(TuiOperationId id, CancellationToken ct)
        {
            gate.Wait(TestTimeout);
            return Task.FromResult("done");
        }

        // act
        var submitted = queue.TrySubmit("slot", BlockingEffect, testToken, out var operationId);
        var elapsed = stopwatch.Elapsed;

        // assert
        Assert.True(submitted);
        Assert.NotEqual(default, operationId);
        Assert.True(elapsed < TimeSpan.FromMilliseconds(500), $"TrySubmit blocked for {elapsed}.");
        Assert.Equal(1, queue.PendingCount);

        gate.Set();
        await WaitUntilAsync(() => queue.PendingCount == 0, testToken);
    }

    [Fact]
    public void TrySubmit_Should_ReturnTrue_When_ResumedAfterStopAccepting()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();
        queue.StopAccepting();
        var rejected = queue.TrySubmit("compose", (_, _) => Task.FromResult("done"), testToken, out _);

        // act
        queue.ResumeAccepting();
        var submitted = queue.TrySubmit("compose", (_, _) => Task.FromResult("done"), testToken, out var operationId);

        // assert
        Assert.False(rejected);
        Assert.True(submitted);
        Assert.NotEqual(default, operationId);
    }

    [Fact]
    public async Task TrySubmit_Should_AllowResubmission_When_PriorEffectUnderSameKeyCompleted()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();

        queue.TrySubmit("compose", (_, _) => Task.FromResult("first"), testToken, out _);
        await WaitUntilAsync(() => queue.PendingCount == 0, testToken);

        // act
        var resubmitted = queue.TrySubmit("compose", (_, _) => Task.FromResult("second"), testToken, out var secondId);

        // assert
        Assert.True(resubmitted);
        Assert.NotEqual(default, secondId);
    }

    [Fact]
    public void TrySubmit_Should_ReturnFalse_When_NotAccepting()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();
        queue.StopAccepting();

        // act
        var submitted = queue.TrySubmit("compose", (_, _) => Task.FromResult("done"), testToken, out var operationId);

        // assert
        Assert.False(submitted);
        Assert.Equal(default, operationId);
    }

    [Fact]
    public async Task DrainCompletions_Should_ReturnCompletedResult_When_EffectSucceeds()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();
        queue.TrySubmit("compose", (_, _) => Task.FromResult("stored"), testToken, out var operationId);

        // act
        await WaitUntilAsync(() => queue.PendingCount == 0, testToken);
        var completions = queue.DrainCompletions();

        // assert
        var completed = Assert.IsType<TuiEffectCompletion<string>.Completed>(Assert.Single(completions));
        Assert.Equal(operationId, completed.OperationId);
        Assert.Equal("stored", completed.Result);
    }

    [Fact]
    public async Task DrainCompletions_Should_ReturnFaulted_When_EffectThrows()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();
        var thrown = new InvalidOperationException("boom");

        Task<string> ThrowingEffect(TuiOperationId id, CancellationToken ct) => throw thrown;

        queue.TrySubmit("compose", ThrowingEffect, testToken, out var operationId);

        // act
        await WaitUntilAsync(() => queue.PendingCount == 0, testToken);
        var completions = queue.DrainCompletions();

        // assert
        // Supervised: the exception became a deterministic completion result instead
        // of an unobserved background-task exception.
        var faulted = Assert.IsType<TuiEffectCompletion<string>.Faulted>(Assert.Single(completions));
        Assert.Equal(operationId, faulted.OperationId);
        Assert.Same(thrown, faulted.Exception);
    }

    [Fact]
    public async Task DrainCompletions_Should_ReturnCancelled_When_EffectObservesCancellation()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();
        using var effectCts = new CancellationTokenSource();

        async Task<string> CancellingEffect(TuiOperationId id, CancellationToken ct)
        {
            await Task.Delay(TestTimeout, ct);
            return "unreachable";
        }

        queue.TrySubmit("compose", CancellingEffect, effectCts.Token, out var operationId);
        await effectCts.CancelAsync();

        // act
        await WaitUntilAsync(() => queue.PendingCount == 0, testToken);
        var completions = queue.DrainCompletions();

        // assert
        var cancelled = Assert.IsType<TuiEffectCompletion<string>.Cancelled>(Assert.Single(completions));
        Assert.Equal(operationId, cancelled.OperationId);
    }

    [Fact]
    public void DrainCompletions_Should_ReturnEmpty_When_NothingCompletedYet()
    {
        // arrange
        var queue = new TuiEffectQueue<string>();

        // act
        var completions = queue.DrainCompletions();

        // assert
        Assert.Empty(completions);
    }

    [Fact]
    public async Task DrainCompletions_Should_ReturnResult_Even_When_NoWakeEventWasEverConsumed()
    {
        // A completion is persisted before any wake event is posted, so it is
        // observable purely by draining, without ever running RunAsync (which is what
        // relays the wake event onto a TuiApplication's channel) or reading anything
        // from a channel at all. This is what makes a dropped wake event harmless.
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();
        queue.TrySubmit("compose", (_, _) => Task.FromResult("stored"), testToken, out _);

        // act
        await WaitUntilAsync(() => queue.PendingCount == 0, testToken);

        // assert
        Assert.Single(queue.DrainCompletions());
    }

    [Fact]
    public async Task PendingOperationIds_Should_ExposeInFlightSubmissions_ForDiscoverability()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();
        var release = new TaskCompletionSource();

        async Task<string> Effect(TuiOperationId id, CancellationToken ct)
        {
            await release.Task.WaitAsync(TestTimeout, ct);
            return "done";
        }

        queue.TrySubmit("compose", Effect, testToken, out var operationId);

        // act
        var pendingIds = queue.PendingOperationIds;

        // assert
        Assert.Equal([operationId], pendingIds);

        release.SetResult();
        await WaitUntilAsync(() => queue.PendingCount == 0, testToken);
    }

    [Fact]
    public async Task DrainPendingAsync_Should_ReturnImmediately_When_NothingInFlight()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();
        var stopwatch = Stopwatch.StartNew();

        // act
        await queue.DrainPendingAsync(TimeSpan.FromSeconds(5), testToken);

        // assert
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task DrainPendingAsync_Should_ReturnOnceEffectCompletes_When_ItFinishesBeforeTheBound()
    {
        // Exercises an effect completing DURING the quit gate's bounded drain.
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();
        var release = new TaskCompletionSource();

        async Task<string> Effect(TuiOperationId id, CancellationToken ct)
        {
            await release.Task.WaitAsync(TestTimeout, ct);
            return "done";
        }

        queue.TrySubmit("compose", Effect, testToken, out _);
        release.SetResult();

        // act
        await queue.DrainPendingAsync(TimeSpan.FromSeconds(5), testToken);

        // assert
        Assert.Equal(0, queue.PendingCount);
    }

    [Fact]
    public async Task DrainPendingAsync_Should_LeavePendingCount_When_EffectOutlivesTheBound()
    {
        // Exercises an effect still running AFTER the quit gate's bounded drain
        // expires: the runtime gives up waiting but does not cancel it.
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();
        var release = new TaskCompletionSource();

        async Task<string> NeverCooperatesWithTheBound(TuiOperationId id, CancellationToken ct)
        {
            // Deliberately ignores the bound; only completes when released below.
            await release.Task;
            return "done";
        }

        queue.TrySubmit("compose", NeverCooperatesWithTheBound, testToken, out var operationId);

        // act
        await queue.DrainPendingAsync(TimeSpan.FromMilliseconds(50), testToken);

        // assert
        Assert.Equal(1, queue.PendingCount);
        Assert.Equal([operationId], queue.PendingOperationIds);

        // cleanup: let the effect resolve so it does not outlive the test.
        release.SetResult();
        await WaitUntilAsync(() => queue.PendingCount == 0, testToken);
    }

    [Fact]
    public async Task RunAsync_Should_PostOneWakeEvent_PerCompletedEffect()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);
        var relayTask = queue.RunAsync(channel.Writer, cts.Token);

        // act
        queue.TrySubmit("compose", (_, _) => Task.FromResult("stored"), testToken, out _);
        var wakeEvent = await channel.Reader.ReadAsync(testToken).AsTask().WaitAsync(TestTimeout, testToken);

        // assert
        Assert.IsType<TuiEvent.EffectCompletedEvent>(wakeEvent);

        await cts.CancelAsync();
        await relayTask;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TestTimeout);

        while (!condition())
        {
            await Task.Delay(5, timeoutCts.Token);
        }
    }
}
