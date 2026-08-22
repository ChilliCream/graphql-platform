using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using ChilliCream.Nitro.CommandLine.Tui.Shell;

namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

internal sealed class BoardMailCommand : Command
{
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
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();

        if (!console.IsInteractive)
        {
            throw new ExitException("agent mail board requires an interactive terminal.");
        }

        var workspaceDirectory = store.FindWorkspaceDirectory()
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        var actor = MailActor.Resolve(null, environmentVariableProvider);
        var mode = new MailMode(store, actor, agentRegistry, timeProvider);
        var dispatcher = new KeyDispatcher(MailKeyMap.CreateDefault());
        var shell = new TuiShell(dispatcher, mode, console.Profile.Width, console.Profile.Height);
        var application = new TuiApplication(console);
        var dbWatcher = new SqliteDbWatcher(AgentWorkspace.GetDatabasePath(workspaceDirectory));

        using var quitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        shell.QuitConfirmed += () => quitCts.Cancel();

        await application.RunAsync(shell.Handle, shell.Render, quitCts.Token, [dbWatcher.RunAsync]);

        return ExitCodes.Success;
    }
}
