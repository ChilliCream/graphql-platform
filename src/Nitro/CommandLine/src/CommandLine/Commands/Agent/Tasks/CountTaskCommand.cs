using ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks;

internal sealed class CountTaskCommand : Command
{
    public CountTaskCommand() : base("count")
    {
        Description = "Count tasks.";

        Options.Add(Opt<TaskByOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent tasks count", "agent tasks count --by status");

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

        var by = parseResult.GetValue(Opt<TaskByOption>.Instance);

        if (string.IsNullOrEmpty(by))
        {
            var total = await store.CountTasksAsync(cancellationToken);

            if (!console.IsHumanReadable)
            {
                resultHolder.SetResult(new ObjectResult(new TaskTotalCountResult(total)));
                return ExitCodes.Success;
            }

            console.WriteLine(total.ToString());

            return ExitCodes.Success;
        }

        var dimension = ParseDimension(by);
        var rows = await store.CountTasksByAsync(dimension, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<TaskCount>(rows));
            return ExitCodes.Success;
        }

        foreach (var row in rows)
        {
            console.WriteLine($"{row.Value}  {row.Count}");
        }

        return ExitCodes.Success;
    }

    public sealed record TaskTotalCountResult(int Total);

    private static TaskCountDimension ParseDimension(string by) => by.Trim().ToLowerInvariant() switch
    {
        "status" => TaskCountDimension.Status,
        "type" => TaskCountDimension.Type,
        "priority" => TaskCountDimension.Priority,
        "assignee" => TaskCountDimension.Assignee,
        "label" => TaskCountDimension.Label,
        _ => throw new ExitException(
            $"Invalid --by value '{by}'. Use status, type, priority, assignee, or label.")
    };
}
