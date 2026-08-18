using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using ChilliCream.Nitro.CommandLine.Tui.Search;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Tree;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class BoardTaskCommand : Command
{
    public BoardTaskCommand() : base("board")
    {
        Description = "Open the interactive task board.";

        this.AddExamples("task board");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();

        if (!console.IsInteractive)
        {
            throw new ExitException("task board requires an interactive terminal.");
        }

        _ = store.FindWorkspaceDirectory()
            ?? throw new ExitException("No task workspace found. Run `nitro task init` first.");

        var actor = TaskActor.Resolve(null, environmentVariableProvider);
        var loader = new BoardDataLoader(store, timeProvider);
        var mode = new BoardMode(loader);
        var searchMode = new SearchMode(store);
        var treeView = new DependencyTreeView(store, rootId: "");
        var dispatcher = new KeyDispatcher(KeyMap.CreateDefaultGlobal());
        var shell = new TuiShell(
            dispatcher,
            mode,
            console.Profile.Width,
            console.Profile.Height,
            searchMode,
            treeView,
            store,
            actor);
        var application = new TuiApplication(console);

        using var quitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        shell.QuitConfirmed += () => quitCts.Cancel();

        await application.RunAsync(shell.Handle, shell.Render, quitCts.Token);

        return ExitCodes.Success;
    }
}
