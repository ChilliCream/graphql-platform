using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
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
    /// Runs the tabbed shell until the user quits or
    /// <paramref name="cancellationToken"/> is cancelled, returning the
    /// shell's exit code.
    /// </summary>
    public static async Task<int> RunAsync(
        INitroConsole console,
        ITaskStore taskStore,
        IMailStore mailStore,
        IMemoryStore memoryStore,
        IAgentRegistry agentRegistry,
        TimeProvider timeProvider,
        IEnvironmentVariableProvider environmentVariableProvider,
        string workspaceDirectory,
        CancellationToken cancellationToken)
    {
        var actor = TaskActor.Resolve(null, environmentVariableProvider);

        var loader = new BoardDataLoader(taskStore, timeProvider);
        var boardMode = new BoardMode(loader);
        var searchMode = new SearchMode(taskStore);
        var treeView = new DependencyTreeView(taskStore, rootId: "");
        var tasksTab = new TuiTab("Tasks", mnemonic: 'T', boardMode, new KeyDispatcher(KeyMap.CreateDefaultGlobal()));

        var mailTab = BuildMailTab(mailStore, timeProvider, environmentVariableProvider);

        var agentsMode = new AgentsMode(agentRegistry, taskStore, mailStore, timeProvider);
        var agentsTab = new TuiTab("Agents", mnemonic: 'A', agentsMode, new KeyDispatcher(KeyMap.CreateDefaultGlobal()));

        var memoryMode = new MemoryMode(memoryStore, timeProvider);
        var memoryTab = new TuiTab("Memory", mnemonic: 'e', memoryMode, new KeyDispatcher(MemoryKeyMap.CreateDefault()));

        var shell = new TuiShell(
            [tasksTab, mailTab, agentsTab, memoryTab],
            console.Profile.Width,
            console.Profile.Height,
            tasksTabIndex: 0,
            searchMode,
            treeView,
            taskStore,
            actor,
            mailStore);
        var application = new TuiApplication(console);
        var dbWatcher = new SqliteDbWatcher(AgentWorkspace.GetDatabasePath(workspaceDirectory));

        using var quitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        shell.QuitConfirmed += () => quitCts.Cancel();

        await application.RunAsync(shell.Handle, shell.Render, quitCts.Token, [dbWatcher.RunAsync]);

        return ExitCodes.Success;
    }

    /// <summary>
    /// Builds the Mail tab from the mail actor resolved against
    /// <paramref name="environmentVariableProvider"/>. When the actor fails
    /// validation (an <see cref="ExitException"/> from
    /// <see cref="MailActor.Resolve"/>), the tab hosts a static
    /// <see cref="MailUnavailableMode"/> instead, so the shell still opens
    /// on the Tasks tab with a working Mail tab title and no unread
    /// polling.
    /// </summary>
    internal static TuiTab BuildMailTab(
        IMailStore mailStore,
        TimeProvider timeProvider,
        IEnvironmentVariableProvider environmentVariableProvider)
    {
        try
        {
            var mailActor = MailActor.Resolve(null, environmentVariableProvider);
            var mailMode = new MailMode(mailStore, mailActor, timeProvider);

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
