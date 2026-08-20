using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class WhereTaskCommand : Command
{
    public WhereTaskCommand() : base("where")
    {
        Description = "Print the absolute path of the current task workspace.";

        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent tasks where");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var workspaceDirectory = store.FindWorkspaceDirectory()
            ?? throw new ExitException(
                "No agent workspace found. Run `nitro agent init` first.");

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new TaskWorkspaceLocationResult(workspaceDirectory)));
            return Task.FromResult(ExitCodes.Success);
        }

        console.WriteLine(workspaceDirectory);

        return Task.FromResult(ExitCodes.Success);
    }

    public sealed record TaskWorkspaceLocationResult(string Path);
}
