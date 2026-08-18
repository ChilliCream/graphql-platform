using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class StatusTaskEpicCommand : Command
{
    public StatusTaskEpicCommand() : base("status")
    {
        Description = "Show epics with their child completion counts.";

        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("task epic status");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var epics = await store.GetEpicStatusesAsync(cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<TaskEpicStatus>(epics));
            return ExitCodes.Success;
        }

        if (epics.Count == 0)
        {
            console.WriteLine("No epics found.");
            return ExitCodes.Success;
        }

        foreach (var epic in epics)
        {
            var line = $"{epic.Id}  {epic.Closed}/{epic.Total}  {epic.Title}";

            if (epic.IsEligibleForClose)
            {
                line += "  (eligible for close)";
            }

            console.WriteLine(line);
        }

        return ExitCodes.Success;
    }
}
