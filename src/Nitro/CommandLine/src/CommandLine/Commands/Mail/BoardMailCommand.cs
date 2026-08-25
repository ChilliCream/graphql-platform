using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using ChilliCream.Nitro.CommandLine.Tui.Shell;

namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

internal sealed class BoardMailCommand : Command
{
    /// <summary>
    /// The bound for the final, unconditional shield drain in
    /// <see cref="ExecuteAsync"/>'s <c>finally</c> block: a Ctrl+C or
    /// host-cancelled exit bypasses <see cref="MailMode.CreateQuitGate"/>
    /// entirely, so this is the only chance a send still in flight gets to
    /// have its already-uncancellable store write actually land before the
    /// process exits; see <see cref="MailMode.ShieldPendingSendsAsync"/>.
    /// </summary>
    private static readonly TimeSpan SendShieldBound = TimeSpan.FromSeconds(2);

    /// <summary>
    /// How often the standalone board's live presence row is touched while
    /// it stays open, so <c>lastHeardAt</c> stays meaningful for a long-running
    /// session without needing a write on every keystroke.
    /// </summary>
    private static readonly TimeSpan BoardSessionTouchInterval = TimeSpan.FromSeconds(30);

    public BoardMailCommand() : base("board")
    {
        Description = "Open the interactive mail board.";

        this.AddExamples("agent mail board");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<IMailStore>();
        var agentRegistry = services.GetRequiredService<IAgentRegistry>();
        var wakeDispatcher = services.GetRequiredService<IActorWakeDispatcher>();
        var wakeObserver = services.GetRequiredService<IMailWakeReceiptObserver>();
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();
        var boardSessionLifecycle = services.GetRequiredService<IBoardSessionLifecycle>();

        if (!console.IsInteractive)
        {
            throw new ExitException("agent mail board requires an interactive terminal.");
        }

        var workspaceDirectory = store.FindWorkspaceDirectory()
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        // Built ahead of MailMode so its own send effects can be plumbed the
        // same shutdown signal (Ctrl+C or the caller's own cancellation)
        // this loop itself runs on; see MailMode's constructor remarks.
        using var quitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var actor = MailActor.Resolve(null, environmentVariableProvider);

        // Live board presence, started before the TUI loop opens and always
        // ended again below (normal quit, cancellation, or this method
        // itself failing) in the outer finally, so the board never leaves a
        // stale live row behind. Closing the board removes only this live
        // row; the durable actor identity and mail history are untouched.
        var boardSession = await boardSessionLifecycle.StartAsync(actor, cancellationToken);

        try
        {
            // The standalone board never starts a daemon of its own, so it is
            // wired with the real dispatcher (the same one the CLI's send/reply
            // commands use) rather than MailMode's dashboard-only no-op.
            var mode = new MailMode(
                store, actor, agentRegistry, wakeDispatcher, wakeObserver, timeProvider, quitCts.Token);
            var dispatcher = new KeyDispatcher(MailKeyMap.CreateDefault());
            var shell = new TuiShell(
                dispatcher,
                mode,
                console.Profile.Width,
                console.Profile.Height,
                actor: $"{actor} ({BoardSessionLifecycle.OperatorRole})",
                quitGates: [mode.CreateQuitGate()]);
            var application = new TuiApplication(console);
            var dbWatcher = new SqliteDbWatcher(AgentWorkspace.GetDatabasePath(workspaceDirectory));

            shell.QuitConfirmed += () => quitCts.Cancel();
            shell.QuitCancelled += mode.ResumeSendAcceptance;

            try
            {
                await application.RunAsync(
                    shell.Handle,
                    shell.Render,
                    quitCts.Token,
                    [
                        dbWatcher.RunAsync,
                        mode.RunSendEffectEventsAsync,
                        (_, token) => RunBoardSessionHeartbeatAsync(boardSessionLifecycle, boardSession, token)
                    ]);
            }
            finally
            {
                // Unconditional: covers the Ctrl+C/host-cancellation path the
                // interactive quit gate above never runs for.
                await mode.ShieldPendingSendsAsync(SendShieldBound, CancellationToken.None);
            }
        }
        finally
        {
            await boardSessionLifecycle.EndAsync(boardSession, CancellationToken.None);
        }

        return ExitCodes.Success;
    }

    /// <summary>
    /// Touches <paramref name="generation"/>'s live presence row on a fixed
    /// interval until <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    private static async Task RunBoardSessionHeartbeatAsync(
        IBoardSessionLifecycle lifecycle, AgentSessionGeneration generation, CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(BoardSessionTouchInterval);

            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await lifecycle.TouchAsync(generation, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
    }
}
