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

    private static TuiShell CreateShell(FakeTuiMode mode, params TuiQuitGate[] quitGates) =>
        new(new KeyDispatcher(KeyMap.CreateDefaultGlobal()), mode, 80, 24, quitGates: quitGates);

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
}
