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
/// Runs the unified Tasks, Mail, Agents, and Memory TUI.
/// </summary>
internal static class AgentTuiLauncher
{
    private static readonly TimeSpan PendingSendShutdownTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan PresenceHeartbeatInterval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Runs the TUI and owns its live-presence session and mail wake daemon.
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
        string actor,
        string workspaceDirectory,
        IMailWakeDaemonCoordinator mailWakeDaemonCoordinator,
        IMailWakeReceiptObserver mailWakeReceiptObserver,
        IBoardSessionLifecycle boardSessionLifecycle,
        CancellationToken cancellationToken)
    {
        AgentSessionGeneration? presenceSession = null;

        try
        {
            presenceSession = await boardSessionLifecycle.StartAsync(actor, cancellationToken);
        }
        catch (ExitException)
        {
            // An invalid mail identity must not prevent the other tabs from opening.
        }

        try
        {
            return await RunShellAsync(
                console, taskStore, mailStore, memoryStore, agentRegistry, agentSessionRegistry, activityReader,
                timeProvider, actor, workspaceDirectory, mailWakeDaemonCoordinator,
                mailWakeReceiptObserver, boardSessionLifecycle, presenceSession, cancellationToken);
        }
        finally
        {
            if (presenceSession is not null)
            {
                await boardSessionLifecycle.EndAsync(presenceSession, CancellationToken.None);
            }
        }
    }

    private static async Task<int> RunShellAsync(
        INitroConsole console,
        ITaskStore taskStore,
        IMailStore mailStore,
        IMemoryStore memoryStore,
        IAgentRegistry agentRegistry,
        IAgentSessionRegistry agentSessionRegistry,
        IClaudeSessionActivityReader activityReader,
        TimeProvider timeProvider,
        string actor,
        string workspaceDirectory,
        IMailWakeDaemonCoordinator mailWakeDaemonCoordinator,
        IMailWakeReceiptObserver mailWakeReceiptObserver,
        IBoardSessionLifecycle boardSessionLifecycle,
        AgentSessionGeneration? presenceSession,
        CancellationToken cancellationToken)
    {
        var searchMode = new SearchMode(taskStore);
        var treeView = new DependencyTreeView(taskStore, rootId: "");

        // The event loop and Mail send effects share one shutdown signal.
        using var quitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var tabs = BuildTabs(
            taskStore,
            mailStore,
            memoryStore,
            agentRegistry,
            agentSessionRegistry,
            activityReader,
            timeProvider,
            actor,
            mailWakeReceiptObserver,
            quitCts.Token);

        // An unavailable Mail tab has no send effects to drain on exit.
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

        // Start only after the shell is ready to report daemon status.
        await mailWakeDaemonCoordinator.StartAsync(cancellationToken);

        var eventSources = new List<TuiEventSource> { dbWatcher.RunAsync };

        if (mailMode is not null)
        {
            eventSources.Add(mailMode.RunSendEffectEventsAsync);
        }

        if (presenceSession is { } livePresenceSession)
        {
            eventSources.Add(
                (_, token) => RunPresenceHeartbeatAsync(boardSessionLifecycle, livePresenceSession, token));
        }

        try
        {
            await application.RunAsync(shell.Handle, shell.Render, quitCts.Token, eventSources);
        }
        finally
        {
            // Stop background delivery even when the event loop fails.
            await mailWakeDaemonCoordinator.StopAsync(CancellationToken.None);

            // Ctrl+C bypasses the quit gate. Give a started store write a
            // brief chance to commit before the process exits.
            if (mailMode is not null)
            {
                await mailMode.ShieldPendingSendsAsync(PendingSendShutdownTimeout, CancellationToken.None);
            }
        }

        return ExitCodes.Success;
    }

    /// <summary>
    /// Refreshes the TUI's live-presence session until shutdown.
    /// </summary>
    private static async Task RunPresenceHeartbeatAsync(
        IBoardSessionLifecycle lifecycle, AgentSessionGeneration generation, CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(PresenceHeartbeatInterval);

            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await lifecycle.TouchAsync(generation, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
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
        string actor,
        IMailWakeReceiptObserver mailWakeReceiptObserver,
        CancellationToken effectCancellationToken = default)
    {
        var loader = new BoardDataLoader(taskStore, timeProvider);
        var boardMode = new BoardMode(loader);
        var tasksTab = new TuiTab("Tasks", mnemonic: 'T', boardMode, new KeyDispatcher(KeyMap.CreateDefaultGlobal()));

        var mailTab = BuildMailTab(
            mailStore, agentRegistry, timeProvider, actor, mailWakeReceiptObserver,
            effectCancellationToken);

        var agentsMode = new AgentsMode(taskStore, mailStore, agentSessionRegistry, activityReader, timeProvider);
        var agentsTab = new TuiTab("Agents", mnemonic: 'A', agentsMode, new KeyDispatcher(KeyMap.CreateDefaultGlobal()));

        var memoryMode = new MemoryMode(memoryStore, timeProvider);
        var memoryTab = new TuiTab("Memory", mnemonic: 'e', memoryMode, new KeyDispatcher(MemoryKeyMap.CreateDefault()));

        return [tasksTab, mailTab, agentsTab, memoryTab];
    }

    /// <summary>
    /// Builds the Mail tab for the already-resolved session actor. Wake
    /// dispatch belongs to the shared daemon; the tab enqueues work and
    /// observes its result.
    /// </summary>
    internal static TuiTab BuildMailTab(
        IMailStore mailStore,
        IAgentRegistry agentRegistry,
        TimeProvider timeProvider,
        string actor,
        IMailWakeReceiptObserver mailWakeReceiptObserver,
        CancellationToken effectCancellationToken = default)
    {
        var mailMode = new MailMode(
            mailStore,
            actor,
            agentRegistry,
            new DaemonOwnedActorWakeDispatcher(),
            new DaemonSettledMailWakeReceiptObserver(mailWakeReceiptObserver, timeProvider),
            timeProvider,
            effectCancellationToken);

        return new TuiTab(
            () => mailMode.UnreadCount > 0 ? $"Mail ({mailMode.UnreadCount})" : "Mail",
            mnemonic: 'M',
            mailMode,
            new KeyDispatcher(MailKeyMap.CreateDefault()));
    }
}
