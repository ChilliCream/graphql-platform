using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class StatsTaskCommand : Command
{
    public StatsTaskCommand() : base("stats")
    {
        Description = "Show summary statistics for the task workspace.";

        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent tasks stats");

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

        var stats = await store.GetStatsAsync(cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(stats));
            return ExitCodes.Success;
        }

        var totalTasks = stats.StatusCounts.Sum(row => row.Count);

        console.WriteLine($"Tasks: {totalTasks}");

        foreach (var row in stats.StatusCounts)
        {
            console.WriteLine($"  {row.Value}: {row.Count}");
        }

        console.WriteLine($"Ready: {stats.ReadyCount}");
        console.WriteLine($"Blocked: {stats.BlockedTaskStatuses.Count}");
        console.WriteLine($"Labels: {stats.LabelCount}");
        console.WriteLine($"Comments: {stats.CommentCount}");
        console.WriteLine($"Events: {stats.EventCount}");

        return ExitCodes.Success;
    }
}
