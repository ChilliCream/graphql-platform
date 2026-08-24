using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Shell;

/// <summary>
/// Covers the pre-cancellation quit gate contract: <see cref="TuiShell"/> runs every
/// registered <see cref="TuiQuitGate"/> before a confirmed normal quit is allowed to
/// raise <see cref="TuiShell.QuitConfirmed"/>.
/// </summary>
public sealed class TuiShellQuitGateTests
{
    private static ConsoleKeyInfo KeyInfo(char keyChar, ConsoleKey key) =>
        new(keyChar, key, shift: false, alt: false, control: false);

    private static readonly TuiEvent.KeyEvent QuitKey = new(KeyInfo('q', ConsoleKey.Q));
    private static readonly TuiEvent.KeyEvent YesKey = new(KeyInfo('y', ConsoleKey.Y));
    private static readonly TuiEvent.KeyEvent NoKey = new(KeyInfo('n', ConsoleKey.N));

    private static readonly TimeSpan ShortDrainBound = TimeSpan.FromMilliseconds(200);

    private static TuiShell CreateShell(FakeTuiMode mode, params TuiQuitGate[] quitGates) =>
        new(new KeyDispatcher(KeyMap.CreateDefaultGlobal()), mode, 80, 24, quitGates: quitGates);

    private static TuiShell CreateShell(FakeTuiMode mode, TimeSpan quitGateDrainBound, params TuiQuitGate[] quitGates) =>
        new(
            new KeyDispatcher(KeyMap.CreateDefaultGlobal()),
            mode,
            80,
            24,
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
        shell.Handle(QuitKey);

        // act
        var dirty = shell.Handle(YesKey);

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
        shell.Handle(QuitKey);

        // act
        var dirty = shell.Handle(YesKey);

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
        shell.Handle(QuitKey);

        // act
        shell.Handle(YesKey);

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
        shell.Handle(QuitKey);
        shell.Handle(YesKey);

        // act
        var dirty = shell.Handle(YesKey);

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
        shell.Handle(QuitKey);
        shell.Handle(YesKey);

        // act
        var dirty = shell.Handle(NoKey);

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
        shell.Handle(QuitKey);
        shell.Handle(YesKey);

        // act
        var dirty = shell.Handle(NoKey);

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
        shell.Handle(QuitKey);

        // act
        var dirty = shell.Handle(NoKey);

        // assert
        Assert.True(dirty);
        Assert.Equal(0, quitCancelledCount);
    }

    [Fact]
    public async Task Handle_Should_ConfirmQuit_When_QueuedEffectCompletesDuringTheGate()
    {
        // Exercises a real TuiEffectQueue wired into the shell as a TuiQuitGate, with
        // the effect resolving DURING the gate's bounded drain.
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var queue = new TuiEffectQueue<string>();
        var release = new TaskCompletionSource();

        async Task<string> Effect(TuiOperationId id, CancellationToken ct)
        {
            await release.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
            return "done";
        }

        queue.TrySubmit("compose", Effect, testToken, out _);

        var shell = CreateShell(new FakeTuiMode(), QueueGate(queue));
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(QuitKey);

        _ = Task.Run(
            async () =>
            {
                await Task.Delay(50, testToken);
                release.SetResult();
            },
            testToken);

        // act
        var dirty = shell.Handle(YesKey);

        // assert
        Assert.True(dirty);
        Assert.True(confirmed);
        Assert.DoesNotContain("stored-but-pending", RenderToText(shell));
    }

    [Fact]
    public async Task Handle_Should_ShowSecondConfirmation_When_QueuedEffectOutlivesTheGate()
    {
        // Exercises a real TuiEffectQueue wired into the shell as a TuiQuitGate, with
        // the effect still running AFTER the gate's bounded drain expires.
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

        var shell = CreateShell(new FakeTuiMode(), ShortDrainBound, QueueGate(queue));
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(QuitKey);

        // act
        var dirty = shell.Handle(YesKey);

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
        var shell = CreateShell(new FakeTuiMode(), ShortDrainBound, ignoringGate);
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(QuitKey);

        // act
        var dirty = shell.Handle(YesKey);

        // assert
        Assert.True(dirty);
        Assert.False(confirmed);
        Assert.Contains("1 outcome-unknown", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_AggregateCounts_AcrossMultipleGates()
    {
        // arrange
        var first = new TuiQuitGateReport(1, 0, [TuiOperationId.New()]);
        var second = new TuiQuitGateReport(0, 2, [TuiOperationId.New(), TuiOperationId.New()]);
        var shell = CreateShell(new FakeTuiMode(), FixedGate(first), FixedGate(second));
        shell.Handle(QuitKey);

        // act
        shell.Handle(YesKey);

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
