using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Preferences;
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
    /// <summary>
    /// The longest the TUI waits on exit for the last board orientation change to be saved.
    /// </summary>
    private static readonly TimeSpan s_orientationSaveTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Runs the TUI and owns its mail wake daemon, loading and saving the board's column
    /// orientation through <paramref name="boardPreferences"/>. The board is an observer:
    /// it takes no actor and refuses every write.
    /// </summary>
    public static Task<int> RunAsync(
        INitroConsole console,
        ITaskStore taskStore,
        IMailStore mailStore,
        IMemoryStore memoryStore,
        IAgentStore agentStore,
        TimeProvider timeProvider,
        string workspaceDirectory,
        IMailWakeDaemonCoordinator mailWakeDaemonCoordinator,
        IBoardPreferencesStore boardPreferences,
        CancellationToken cancellationToken)
        => RunShellAsync(
            console, taskStore, mailStore, memoryStore, agentStore,
            timeProvider, workspaceDirectory, mailWakeDaemonCoordinator, boardPreferences, cancellationToken);

    private static async Task<int> RunShellAsync(
        INitroConsole console,
        ITaskStore taskStore,
        IMailStore mailStore,
        IMemoryStore memoryStore,
        IAgentStore agentStore,
        TimeProvider timeProvider,
        string workspaceDirectory,
        IMailWakeDaemonCoordinator mailWakeDaemonCoordinator,
        IBoardPreferencesStore boardPreferences,
        CancellationToken cancellationToken)
    {
        // The saved choice is a small local file, so it is read even when the run is already cancelled.
        var orientation = await boardPreferences.ReadOrientationAsync(CancellationToken.None);
        var searchMode = new SearchMode(taskStore);
        var treeView = new DependencyTreeView(taskStore, rootId: "");

        using var quitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var orientationWriter = new BoardOrientationWriter(boardPreferences);
        var tabs = BuildTabs(
            taskStore, mailStore, memoryStore, agentStore, timeProvider, orientationWriter, orientation);

        var shell = new TuiShell(
            tabs,
            console.Profile.Width,
            console.Profile.Height,
            agentStore: agentStore,
            tasksTabIndex: 0,
            searchMode: searchMode,
            treeView: treeView,
            store: taskStore,
            actor: null,
            mailWakeDaemonState: () => mailWakeDaemonCoordinator.Status.State);
        var application = new TuiApplication(console);
        var dbWatcher = new SqliteDbWatcher(AgentWorkspace.GetDatabasePath(workspaceDirectory));

        shell.QuitConfirmed += quitCts.Cancel;

        // Start only after the shell is ready to report daemon status.
        await mailWakeDaemonCoordinator.StartAsync(cancellationToken);

        var eventSources = new List<TuiEventSource> { dbWatcher.RunAsync };

        try
        {
            await application.RunAsync(shell.Handle, shell.Render, quitCts.Token, eventSources);
        }
        finally
        {
            // Stop background delivery even when the event loop fails.
            await mailWakeDaemonCoordinator.StopAsync(CancellationToken.None);

            // Persist the last orientation change before exit, bounded so a stuck disk cannot hang it.
            await orientationWriter.DrainAsync(s_orientationSaveTimeout);
        }

        return ExitCodes.Success;
    }

    /// <summary>
    /// Builds the Tasks, Mail, Agents, and Memory tabs in the order the
    /// shell's tab strip renders them. The board starts in
    /// <paramref name="boardOrientation"/> and hands each change to
    /// <paramref name="orientationWriter"/>.
    /// </summary>
    internal static TuiTab[] BuildTabs(
        ITaskStore taskStore,
        IMailStore mailStore,
        IMemoryStore memoryStore,
        IAgentStore agentStore,
        TimeProvider timeProvider,
        BoardOrientationWriter orientationWriter,
        BoardOrientation boardOrientation)
    {
        var loader = new BoardDataLoader(taskStore, timeProvider);
        var boardMode = new BoardMode(loader, orientation: boardOrientation);

        // A failed save keeps the in-memory choice, and saving never runs on the render loop.
        boardMode.OrientationChanged += orientationWriter.Enqueue;

        var tasksTab = new TuiTab("Tasks", mnemonic: 'T', boardMode, new KeyDispatcher(KeyMap.CreateDefaultGlobal()));

        var mailTab = BuildMailTab(mailStore, agentStore, timeProvider);

        var agentsMode = new AgentsMode(agentStore, mailStore, taskStore, memoryStore, timeProvider);
        var agentsTab = new TuiTab("Agents", mnemonic: 'A', agentsMode, new KeyDispatcher(KeyMap.CreateDefaultGlobal()));

        var memoryMode = new MemoryMode(memoryStore, timeProvider);
        var memoryTab = new TuiTab("Memory", mnemonic: 'e', memoryMode, new KeyDispatcher(MemoryKeyMap.CreateDefault()));

        return [tasksTab, mailTab, agentsTab, memoryTab];
    }

    /// <summary>
    /// Builds the Mail tab: a read-only table of every workspace thread. The board has no
    /// acting agent, so the tab title never carries an unread count.
    /// </summary>
    internal static TuiTab BuildMailTab(
        IMailStore mailStore,
        IAgentStore agentStore,
        TimeProvider timeProvider)
    {
        var mailMode = new MailMode(mailStore, agentStore, timeProvider);

        return new TuiTab(
            "Mail",
            mnemonic: 'M',
            mailMode,
            new KeyDispatcher(MailKeyMap.CreateDefault()));
    }
}
