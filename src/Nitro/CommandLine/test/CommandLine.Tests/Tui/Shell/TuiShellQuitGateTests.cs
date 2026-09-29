using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Shell;

/// <summary>
/// Covers the pre-cancellation quit gate contract: <see cref="TuiShell"/> runs every
/// registered <see cref="TuiQuitGate"/> before a confirmed normal quit is allowed to
/// raise <see cref="TuiShell.QuitConfirmed"/>. Includes shell-level coverage of a real
/// <see cref="TuiEffectQueue{TResult}"/> wired in as a gate, with its effect completing
/// before, during, and after the gate's bounded drain.
/// </summary>
public sealed class TuiShellQuitGateTests
{
    private static ConsoleKeyInfo KeyInfo(char keyChar, ConsoleKey key) =>
        new(keyChar, key, shift: false, alt: false, control: false);

    private static readonly TuiEvent.KeyEvent s_quitKey = new(KeyInfo('q', ConsoleKey.Q));
    private static readonly TuiEvent.KeyEvent s_yesKey = new(KeyInfo('y', ConsoleKey.Y));
    private static readonly TuiEvent.KeyEvent s_noKey = new(KeyInfo('n', ConsoleKey.N));

    private static readonly TimeSpan s_shortDrainBound = TimeSpan.FromMilliseconds(200);

    private static TuiShell CreateShell(FakeTuiMode mode, params TuiQuitGate[] quitGates) =>
        new(
            new KeyDispatcher(KeyMap.CreateDefaultGlobal()),
            mode,
            80,
            24,
            agentStore: new Agents.FakeAgentStore(TimeProvider.System),
            quitGates: quitGates);

    private static TuiShell CreateShell(FakeTuiMode mode, TimeSpan quitGateDrainBound, params TuiQuitGate[] quitGates) =>
        new(
            new KeyDispatcher(KeyMap.CreateDefaultGlobal()),
            mode,
            80,
            24,
            agentStore: new Agents.FakeAgentStore(TimeProvider.System),
            quitGates: quitGates,
            quitGateDrainBound: quitGateDrainBound);

    private static TuiQuitGate QueueGate(TuiEffectQueue<string> queue, int outcomeUnknown = 0) =>
        async (bound, ct) =>
        {
            queue.StopAccepting();
            await queue.DrainPendingAsync(bound, ct);
            return new TuiQuitGateReport(queue.PendingCount, outcomeUnknown, queue.PendingOperationIds);
        };

    private static string RenderToText(TuiShell shell)
    {
        var console = new TestConsole().Width(80);
        console.Write(shell.Render());
        return console.Output;
    }

    private static TuiQuitGate FixedGate(TuiQuitGateReport report, List<TuiQuitGateReport>? invocations = null) =>
        (_, _) =>
        {
            invocations?.Add(report);
            return Task.FromResult(report);
        };

    [Fact]
    public void Handle_Should_RaiseQuitConfirmed_Immediately_When_GateReportsNoUnresolvedWork()
    {
        // arrange
        var shell = CreateShell(new FakeTuiMode(), FixedGate(TuiQuitGateReport.Clear));
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(s_quitKey);

        // act
        var dirty = shell.Handle(s_yesKey);

        // assert
        Assert.True(dirty);
        Assert.True(confirmed);
    }

    [Fact]
    public void Handle_Should_ShowSecondConfirmation_WithoutQuitting_When_GateReportsPendingWork()
    {
        // arrange
        var report = new TuiQuitGateReport(2, 0, [TuiOperationId.New(), TuiOperationId.New()]);
        var shell = CreateShell(new FakeTuiMode(), FixedGate(report));
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(s_quitKey);

        // act
        var dirty = shell.Handle(s_yesKey);

        // assert
        Assert.True(dirty);
        Assert.False(confirmed);
        var text = RenderToText(shell);
        Assert.Contains("2 stored-but-pending", text);
        Assert.Contains("0 outcome-unknown", text);
    }

    [Fact]
    public void Handle_Should_ShowSecondConfirmation_WithoutQuitting_When_GateReportsOutcomeUnknownWork()
    {
        // arrange
        var report = new TuiQuitGateReport(0, 1, [TuiOperationId.New()]);
        var shell = CreateShell(new FakeTuiMode(), FixedGate(report));
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(s_quitKey);

        // act
        shell.Handle(s_yesKey);

        // assert
        Assert.False(confirmed);
        Assert.Contains("1 outcome-unknown", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_RaiseQuitConfirmed_When_SecondConfirmationIsConfirmed()
    {
        // arrange
        var report = new TuiQuitGateReport(1, 0, [TuiOperationId.New()]);
        var invocations = new List<TuiQuitGateReport>();
        var shell = CreateShell(new FakeTuiMode(), FixedGate(report, invocations));
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(s_quitKey);
        shell.Handle(s_yesKey);

        // act
        var dirty = shell.Handle(s_yesKey);

        // assert
        Assert.True(dirty);
        Assert.True(confirmed);
        // The gate ran once for the first confirmation; the second confirmation
        // trusts that result rather than draining again.
        Assert.Single(invocations);
    }

    [Fact]
    public void Handle_Should_NotQuit_When_SecondConfirmationIsCancelled()
    {
        // arrange
        var report = new TuiQuitGateReport(1, 0, [TuiOperationId.New()]);
        var shell = CreateShell(new FakeTuiMode(), FixedGate(report));
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(s_quitKey);
        shell.Handle(s_yesKey);

        // act
        var dirty = shell.Handle(s_noKey);

        // assert
        Assert.True(dirty);
        Assert.False(confirmed);
        Assert.DoesNotContain("stored-but-pending", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_RaiseQuitCancelled_When_SecondConfirmationIsCancelled()
    {
        // arrange
        var report = new TuiQuitGateReport(1, 0, [TuiOperationId.New()]);
        var shell = CreateShell(new FakeTuiMode(), FixedGate(report));
        var quitConfirmed = false;
        var quitCancelledCount = 0;
        shell.QuitConfirmed += () => quitConfirmed = true;
        shell.QuitCancelled += () => quitCancelledCount++;
        shell.Handle(s_quitKey);
        shell.Handle(s_yesKey);

        // act
        var dirty = shell.Handle(s_noKey);

        // assert
        Assert.True(dirty);
        Assert.Equal(1, quitCancelledCount);
        Assert.False(quitConfirmed);
    }

    [Fact]
    public void Handle_Should_NotRaiseQuitCancelled_When_FirstConfirmationIsCancelled()
    {
        // arrange
        var shell = CreateShell(new FakeTuiMode());
        var quitCancelledCount = 0;
        shell.QuitCancelled += () => quitCancelledCount++;
        shell.Handle(s_quitKey);

        // act
        var dirty = shell.Handle(s_noKey);

        // assert
        Assert.True(dirty);
        Assert.Equal(0, quitCancelledCount);
    }

    [Fact]
    public async Task Handle_Should_ConfirmQuit_When_QueuedEffectCompletesDuringTheGate()
    {
        // arrange: a real TuiEffectQueue wired in as a gate, with the effect resolving during the drain
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();
        var effectEntered = new TaskCompletionSource();
        var release = new TaskCompletionSource();
        var gateEntered = new TaskCompletionSource();
        IReadOnlyList<TuiEffectCompletion<string>> completions = [];

        async Task<string> Effect(TuiOperationId id, CancellationToken ct)
        {
            effectEntered.SetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
            return "done";
        }

        TuiQuitGate gate = async (bound, ct) =>
        {
            queue.StopAccepting();
            gateEntered.SetResult();
            await queue.DrainPendingAsync(bound, ct);
            completions = queue.DrainCompletions();
            var outcomeUnknownCount = completions.Count(
                completion => completion is TuiEffectCompletion<string>.Faulted or TuiEffectCompletion<string>.Cancelled);
            return new TuiQuitGateReport(queue.PendingCount, outcomeUnknownCount, queue.PendingOperationIds);
        };

        queue.TrySubmit("compose", Effect, testToken, out _);
        await effectEntered.Task.WaitAsync(testToken);

        var shell = CreateShell(new FakeTuiMode(), gate);
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(s_quitKey);

        // act
        var handleTask = Task.Run(() => shell.Handle(s_yesKey), testToken);
        await gateEntered.Task.WaitAsync(testToken);
        release.SetResult();
        var dirty = await handleTask;

        // assert
        Assert.True(dirty);
        Assert.True(confirmed);
        var completed = Assert.IsType<TuiEffectCompletion<string>.Completed>(Assert.Single(completions));
        Assert.Equal("done", completed.Result);
    }

    [Fact]
    public async Task Handle_Should_ShowSecondConfirmation_When_QueuedEffectOutlivesTheGate()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();
        var release = new TaskCompletionSource();

        async Task<string> NeverCooperatesWithTheBound(TuiOperationId id, CancellationToken ct)
        {
            await release.Task;
            return "done";
        }

        queue.TrySubmit("compose", NeverCooperatesWithTheBound, testToken, out _);

        var shell = CreateShell(new FakeTuiMode(), s_shortDrainBound, QueueGate(queue));
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(s_quitKey);

        // act
        var dirty = shell.Handle(s_yesKey);

        // assert
        Assert.True(dirty);
        Assert.False(confirmed);
        Assert.Contains("1 stored-but-pending", RenderToText(shell));

        // cleanup: let the effect resolve so it does not outlive the test.
        release.SetResult();
        await WaitUntilAsync(() => queue.PendingCount == 0, testToken);
    }

    [Fact]
    public void Handle_Should_TreatGateAsOutcomeUnknown_When_GateIgnoresDrainBound()
    {
        // arrange
        TuiQuitGate ignoringGate = (_, _) => new TaskCompletionSource<TuiQuitGateReport>().Task;
        var shell = CreateShell(new FakeTuiMode(), s_shortDrainBound, ignoringGate);
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(s_quitKey);

        // act
        var dirty = shell.Handle(s_yesKey);

        // assert
        Assert.True(dirty);
        Assert.False(confirmed);
        Assert.Contains("1 outcome-unknown", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_TreatGateAsOutcomeUnknown_When_GateReturnsFaultedTask()
    {
        // arrange
        TuiQuitGate gate = (_, _) => Task.FromException<TuiQuitGateReport>(new InvalidOperationException("boom"));
        var shell = CreateShell(new FakeTuiMode(), gate);
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(s_quitKey);

        // act
        var dirty = shell.Handle(s_yesKey);

        // assert
        Assert.True(dirty);
        Assert.False(confirmed);
        Assert.Contains("1 outcome-unknown", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_TreatGateAsOutcomeUnknown_When_GateThrowsSynchronously()
    {
        // arrange
        TuiQuitGate gate = (_, _) => throw new ObjectDisposedException("store");
        var shell = CreateShell(new FakeTuiMode(), gate);
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(s_quitKey);

        // act
        var dirty = shell.Handle(s_yesKey);

        // assert
        Assert.True(dirty);
        Assert.False(confirmed);
        Assert.Contains("1 outcome-unknown", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_NotSuppressHealthyGatePendingCount_When_AnotherGateFaults()
    {
        // arrange
        var healthy = new TuiQuitGateReport(1, 0, [TuiOperationId.New()]);
        TuiQuitGate faulting = (_, _) => throw new InvalidOperationException("boom");
        var shell = CreateShell(new FakeTuiMode(), FixedGate(healthy), faulting);
        shell.Handle(s_quitKey);

        // act
        shell.Handle(s_yesKey);

        // assert
        var text = RenderToText(shell);
        Assert.Contains("1 stored-but-pending", text);
        Assert.Contains("1 outcome-unknown", text);
    }

    [Fact]
    public async Task Handle_Should_ConfirmQuit_When_QueuedEffectCompletedBeforeTheGate()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();

        static Task<string> Effect(TuiOperationId id, CancellationToken ct) => Task.FromResult("done");

        queue.TrySubmit("compose", Effect, testToken, out _);
        await WaitUntilAsync(() => queue.PendingCount == 0, testToken);

        var shell = CreateShell(new FakeTuiMode(), QueueGate(queue));
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(s_quitKey);

        // act
        var dirty = shell.Handle(s_yesKey);

        // assert
        Assert.True(dirty);
        Assert.True(confirmed);
        Assert.DoesNotContain("stored-but-pending", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_AggregateCounts_AcrossMultipleGates()
    {
        // arrange
        var first = new TuiQuitGateReport(1, 0, [TuiOperationId.New()]);
        var second = new TuiQuitGateReport(0, 2, [TuiOperationId.New(), TuiOperationId.New()]);
        var shell = CreateShell(new FakeTuiMode(), FixedGate(first), FixedGate(second));
        shell.Handle(s_quitKey);

        // act
        shell.Handle(s_yesKey);

        // assert
        var text = RenderToText(shell);
        Assert.Contains("1 stored-but-pending", text);
        Assert.Contains("2 outcome-unknown", text);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        while (!condition())
        {
            await Task.Delay(5, timeoutCts.Token);
        }
    }
}
