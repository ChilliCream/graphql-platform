using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Agents;
using ChilliCream.Nitro.CommandLine.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using ChilliCream.Nitro.CommandLine.Tui.Search;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Tree;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

/// <summary>
/// Builds and runs the unified agent TUI: a tabbed <see cref="TuiShell"/>
/// hosting the tasks board, mail board, agent registry list, and memory
/// board over the unified workspace database, starting on the tasks tab,
/// and sharing one <see cref="SqliteDbWatcher"/> instance between all four
/// tabs.
/// </summary>
internal static class AgentTuiLauncher
{
    /// <summary>
    /// The bound for the final, unconditional shield drain in
    /// <see cref="RunAsync"/>'s <c>finally</c> block; see
    /// <see cref="MailMode.ShieldPendingSendsAsync"/>.
    /// </summary>
    private static readonly TimeSpan SendShieldBound = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Runs the tabbed shell until the user quits or
    /// <paramref name="cancellationToken"/> is cancelled, returning the
    /// shell's exit code. Owns <paramref name="mailWakeDaemonCoordinator"/>'s
    /// lifetime for exactly this run: started once the shell is built, and
    /// always stopped again before returning, on every exit path alike
    /// (normal quit, Ctrl+C, cancellation, or the application loop itself
    /// throwing), so a noncooperative caller can never observe the
    /// coordinator still running once this method has returned or thrown.
    /// The daemon's own leadership and health are never fatal to this run:
    /// a coordinator that never reaches <see cref="MailWakeDaemonState.Ready"/>
    /// still leaves every other tab fully usable, only surfaced through the
    /// shell's own footer badge.
    /// </summary>
    public static async Task<int> RunAsync(
        INitroConsole console,
        ITaskStore taskStore,
        IMailStore mailStore,
        IMemoryStore memoryStore,
        IAgentRegistry agentRegistry,
        IAgentSessionRegistry agentSessionRegistry,
        IClaudeSessionActivityReader activityReader,
        TimeProvider timeProvider,
        IEnvironmentVariableProvider environmentVariableProvider,
        string workspaceDirectory,
        IMailWakeDaemonCoordinator mailWakeDaemonCoordinator,
        IMailWakeReceiptObserver mailWakeReceiptObserver,
        CancellationToken cancellationToken)
    {
        var actor = TaskActor.Resolve(null, environmentVariableProvider);

        var searchMode = new SearchMode(taskStore);
        var treeView = new DependencyTreeView(taskStore, rootId: "");

        // Built ahead of BuildTabs so the mail tab's own send effects can be
        // plumbed the same shutdown signal (Ctrl+C or the caller's own
        // cancellation) this loop itself runs on; see MailMode's
        // constructor remarks.
        using var quitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var tabs = BuildTabs(
            taskStore,
            mailStore,
            memoryStore,
            agentRegistry,
            agentSessionRegistry,
            activityReader,
            timeProvider,
            environmentVariableProvider,
            mailWakeReceiptObserver,
            quitCts.Token);

        // A working Mail tab hosts a MailMode (an invalid actor hosts
        // MailUnavailableMode instead, see BuildMailTab); only the former
        // needs a quit gate.
        var mailMode = tabs.Select(t => t.RootMode).OfType<MailMode>().FirstOrDefault();

        var shell = new TuiShell(
            tabs,
            console.Profile.Width,
            console.Profile.Height,
            tasksTabIndex: 0,
            searchMode,
            treeView,
            taskStore,
            actor,
            mailStore,
            mailWakeDaemonState: () => mailWakeDaemonCoordinator.Status.State,
            quitGates: mailMode is null ? null : [mailMode.CreateQuitGate()]);
        var application = new TuiApplication(console);
        var dbWatcher = new SqliteDbWatcher(AgentWorkspace.GetDatabasePath(workspaceDirectory));

        shell.QuitConfirmed += () => quitCts.Cancel();

        if (mailMode is not null)
        {
            shell.QuitCancelled += mailMode.ResumeSendAcceptance;
        }

        // Started outside TuiApplication's own loop, after the shell exists
        // but before it runs, so a startup failure building the shell above
        // never leaves the coordinator running with nothing to report its
        // status to. StartAsync only launches the background run loop and
        // returns immediately (it does not wait for an election outcome),
        // so this never delays the dashboard opening.
        await mailWakeDaemonCoordinator.StartAsync(cancellationToken);

        try
        {
            await application.RunAsync(shell.Handle, shell.Render, quitCts.Token, [dbWatcher.RunAsync]);
        }
        finally
        {
            // Runs on every exit path, including the application loop
            // itself throwing: stops admission, releases leadership if
            // held, and cancels every in-flight actor dispatch, bounded by
            // the coordinator's own shutdown budget rather than this
            // method's cancellation token (already cancelled or cancelling
            // on most of these paths).
            await mailWakeDaemonCoordinator.StopAsync(CancellationToken.None);

            // Unconditional, mirroring BoardMailCommand: covers the
            // Ctrl+C/host-cancellation path the interactive quit gate above
            // never runs for.
            if (mailMode is not null)
            {
                await mailMode.ShieldPendingSendsAsync(SendShieldBound, CancellationToken.None);
            }
        }

        return ExitCodes.Success;
    }

    /// <summary>
    /// Builds the Tasks, Mail, Agents, and Memory tabs in the order the
    /// shell's tab strip renders them.
    /// </summary>
    internal static TuiTab[] BuildTabs(
        ITaskStore taskStore,
        IMailStore mailStore,
        IMemoryStore memoryStore,
        IAgentRegistry agentRegistry,
        IAgentSessionRegistry agentSessionRegistry,
        IClaudeSessionActivityReader activityReader,
        TimeProvider timeProvider,
        IEnvironmentVariableProvider environmentVariableProvider,
        IMailWakeReceiptObserver mailWakeReceiptObserver,
        CancellationToken effectCancellationToken = default)
    {
        var loader = new BoardDataLoader(taskStore, timeProvider);
        var boardMode = new BoardMode(loader);
        var tasksTab = new TuiTab("Tasks", mnemonic: 'T', boardMode, new KeyDispatcher(KeyMap.CreateDefaultGlobal()));

        var mailTab = BuildMailTab(
            mailStore, agentRegistry, timeProvider, environmentVariableProvider, mailWakeReceiptObserver,
            effectCancellationToken);

        var agentsMode = new AgentsMode(taskStore, mailStore, agentSessionRegistry, activityReader, timeProvider);
        var agentsTab = new TuiTab("Agents", mnemonic: 'A', agentsMode, new KeyDispatcher(KeyMap.CreateDefaultGlobal()));

        var memoryMode = new MemoryMode(memoryStore, timeProvider);
        var memoryTab = new TuiTab("Memory", mnemonic: 'e', memoryMode, new KeyDispatcher(MemoryKeyMap.CreateDefault()));

        return [tasksTab, mailTab, agentsTab, memoryTab];
    }

    /// <summary>
    /// Builds the Mail tab from the mail actor resolved against
    /// <paramref name="environmentVariableProvider"/>. When the actor fails
    /// validation (an <see cref="ExitException"/> from
    /// <see cref="MailActor.Resolve"/>), the tab hosts a static
    /// <see cref="MailUnavailableMode"/> instead, so the shell still opens
    /// on the Tasks tab with a working Mail tab title and no unread
    /// polling. A working tab's <see cref="MailMode"/> is wired with
    /// <see cref="DaemonOwnedActorWakeDispatcher"/> rather than a real
    /// <see cref="IActorWakeDispatcher"/>: the unified dashboard's own
    /// <see cref="IMailWakeDaemonCoordinator"/> already owns dispatch for
    /// the whole session, so a compose or reply here only needs to enqueue
    /// and observe, never dispatch directly; see <see cref="MailMode"/>'s
    /// constructor remarks.
    /// </summary>
    internal static TuiTab BuildMailTab(
        IMailStore mailStore,
        IAgentRegistry agentRegistry,
        TimeProvider timeProvider,
        IEnvironmentVariableProvider environmentVariableProvider,
        IMailWakeReceiptObserver mailWakeReceiptObserver,
        CancellationToken effectCancellationToken = default)
    {
        try
        {
            var mailActor = MailActor.Resolve(null, environmentVariableProvider);
            var mailMode = new MailMode(
                mailStore,
                mailActor,
                agentRegistry,
                new DaemonOwnedActorWakeDispatcher(),
                mailWakeReceiptObserver,
                timeProvider,
                effectCancellationToken);

            return new TuiTab(
                () => mailMode.UnreadCount > 0 ? $"Mail ({mailMode.UnreadCount})" : "Mail",
                mnemonic: 'M',
                mailMode,
                new KeyDispatcher(MailKeyMap.CreateDefault()));
        }
        catch (ExitException exception)
        {
            var mode = new MailUnavailableMode(exception.Message);

            return new TuiTab("Mail", mnemonic: 'M', mode, new KeyDispatcher(MailKeyMap.CreateDefault()));
        }
    }
}
