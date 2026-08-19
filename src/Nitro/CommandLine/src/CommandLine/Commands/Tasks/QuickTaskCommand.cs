using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class QuickTaskCommand : Command
{
    public QuickTaskCommand() : base("q")
    {
        Description = "Quickly create a task and print only its ID.";

        Arguments.Add(Opt<TaskTitleArgument>.Instance);

        Options.Add(Opt<TaskPriorityOption>.Instance);
        Options.Add(Opt<TaskTypeOption>.Instance);
        Options.Add(Opt<TaskLabelOption>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "task q \"Fix the parser\"",
            "task q \"Fix the parser\" --priority p1 --type bug --label api");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();
        var environmentVariables = services.GetRequiredService<IEnvironmentVariableProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var title = parseResult.GetRequiredValue(Opt<TaskTitleArgument>.Instance);

        if (title.Length is 0 or > 500)
        {
            throw new ExitException("The title must be 1-500 characters.");
        }

        var priorityValue = parseResult.GetValue(Opt<TaskPriorityOption>.Instance);
        var priority = priorityValue is null
            ? TaskPriorities.Medium
            : TaskPriorities.Parse(priorityValue);

        var typeValue = parseResult.GetValue(Opt<TaskTypeOption>.Instance);
        var type = typeValue is null ? TaskTypes.Task : TaskTypes.Normalize(typeValue);

        var labels = (parseResult.GetValue(Opt<TaskLabelOption>.Instance) ?? [])
            .Select(label => label.Trim().ToLowerInvariant())
            .Distinct()
            .ToArray();

        if (labels.Any(string.IsNullOrEmpty))
        {
            throw new ExitException("Labels must be non-empty.");
        }

        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariables);

        var result = await store.CreateTaskAsync(
            new TaskCreation
            {
                Title = title,
                Priority = priority,
                Type = type,
                Labels = labels,
                Actor = actor
            },
            cancellationToken);

        if (!console.IsHumanReadable)
        {
            var createdTask = await store.GetRequiredTaskAsync(result.Id, cancellationToken);

            resultHolder.SetResult(
                new ObjectResult(TaskSnapshotResult.From(createdTask, result.BlockedBy)));

            return ExitCodes.Success;
        }

        console.WriteLine(result.Id);

        return ExitCodes.Success;
    }
}
