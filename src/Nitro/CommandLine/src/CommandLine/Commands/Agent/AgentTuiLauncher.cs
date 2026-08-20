using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using ChilliCream.Nitro.CommandLine.Tui.Search;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Tree;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

/// <summary>
/// Builds and runs the unified agent TUI: a tabbed <see cref="TuiShell"/>
/// hosting the tasks board and mail board over the unified workspace
/// database, starting on the tasks tab, and sharing one
/// <see cref="SqliteDbWatcher"/> instance between both tabs.
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
        TimeProvider timeProvider,
        IEnvironmentVariableProvider environmentVariableProvider,
        string workspaceDirectory,
        CancellationToken cancellationToken)
    {
        var actor = TaskActor.Resolve(null, environmentVariableProvider);
        var mailActor = MailActor.Resolve(null, environmentVariableProvider);

        var loader = new BoardDataLoader(taskStore, timeProvider);
        var boardMode = new BoardMode(loader);
        var searchMode = new SearchMode(taskStore);
        var treeView = new DependencyTreeView(taskStore, rootId: "");
        var tasksTab = new TuiTab("Tasks", boardMode, new KeyDispatcher(KeyMap.CreateDefaultGlobal()));

        var mailMode = new MailMode(mailStore, mailActor, timeProvider);
        var mailTab = new TuiTab(
            () => mailMode.UnreadCount > 0 ? $"Mail ({mailMode.UnreadCount})" : "Mail",
            mailMode,
            new KeyDispatcher(MailKeyMap.CreateDefault()));

        var shell = new TuiShell(
            [tasksTab, mailTab],
            console.Profile.Width,
            console.Profile.Height,
            tasksTabIndex: 0,
            searchMode,
            treeView,
            taskStore,
            actor);
        var application = new TuiApplication(console);
        var dbWatcher = new SqliteDbWatcher(AgentWorkspace.GetDatabasePath(workspaceDirectory));

        using var quitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        shell.QuitConfirmed += () => quitCts.Cancel();

        await application.RunAsync(shell.Handle, shell.Render, quitCts.Token, [dbWatcher.RunAsync]);

        return ExitCodes.Success;
    }
}
