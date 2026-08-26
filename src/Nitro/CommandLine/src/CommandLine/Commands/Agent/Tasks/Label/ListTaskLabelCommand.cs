using ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks;

internal sealed class ListTaskLabelCommand : Command
{
    public ListTaskLabelCommand() : base("list")
    {
        Description = "List a task's labels, or every label in use.";

        Arguments.Add(Opt<OptionalTaskIdArgument>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent tasks label list", "agent tasks label list \"acme-1a2\"");

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

        var id = parseResult.GetValue(Opt<OptionalTaskIdArgument>.Instance);

        if (id is not null)
        {
            var task = await store.GetRequiredTaskAsync(id, cancellationToken);
            var labels = await store.GetLabelsAsync(task.Id, cancellationToken);

            if (!console.IsHumanReadable)
            {
                var items = labels.Select(label => new TaskLabelCount(label, 1)).ToList();
                resultHolder.SetResult(new ListResult<TaskLabelCount>(items));
                return ExitCodes.Success;
            }

            if (labels.Count == 0)
            {
                console.WriteLine("No labels.");
                return ExitCodes.Success;
            }

            foreach (var label in labels)
            {
                console.WriteLine(label);
            }

            return ExitCodes.Success;
        }

        var rows = await store.GetLabelCountsAsync(cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<TaskLabelCount>(rows));
            return ExitCodes.Success;
        }

        if (rows.Count == 0)
        {
            console.WriteLine("No labels.");
            return ExitCodes.Success;
        }

        foreach (var row in rows)
        {
            console.WriteLine($"{row.Label}  {row.Count}");
        }

        return ExitCodes.Success;
    }
}
