using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class WhereTaskCommand : Command
{
    public WhereTaskCommand() : base("where")
    {
        Description = "Print the absolute path of the current task workspace.";

        this.AddExamples("task where");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();

        var workspaceDirectory = store.FindWorkspaceDirectory()
            ?? throw new ExitException(
                "No task workspace found. Run `nitro task init` first.");

        console.WriteLine(workspaceDirectory);

        return Task.FromResult(ExitCodes.Success);
    }
}
