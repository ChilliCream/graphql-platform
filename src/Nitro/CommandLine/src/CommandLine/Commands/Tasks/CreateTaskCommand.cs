using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class CreateTaskCommand : Command
{
    public CreateTaskCommand() : base("create")
    {
        Description = "Create a task.";

        Arguments.Add(Opt<TaskTitleArgument>.Instance);

        Options.Add(Opt<TaskDescriptionOption>.Instance);
        Options.Add(Opt<TaskPriorityOption>.Instance);
        Options.Add(Opt<TaskTypeOption>.Instance);
        Options.Add(Opt<TaskAssigneeOption>.Instance);
        Options.Add(Opt<TaskLabelOption>.Instance);
        Options.Add(Opt<TaskDueOption>.Instance);
        Options.Add(Opt<TaskDeferUntilOption>.Instance);
        Options.Add(Opt<TaskEstimateOption>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);
        Options.Add(Opt<TaskDependsOnOption>.Instance);
        Options.Add(Opt<TaskParentOption>.Instance);

        this.AddExamples(
            "task create \"Fix the parser\"",
            "task create \"Fix the parser\" --priority p1 --type bug --depends-on \"acme-9z8\"");

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

        var dueValue = parseResult.GetValue(Opt<TaskDueOption>.Instance);
        var dueAt = dueValue is null
            ? (DateTimeOffset?)null
            : TaskDates.Parse(dueValue, Opt<TaskDueOption>.Instance.Name);

        var deferValue = parseResult.GetValue(Opt<TaskDeferUntilOption>.Instance);
        var deferUntil = deferValue is null
            ? (DateTimeOffset?)null
            : TaskDates.Parse(deferValue, Opt<TaskDeferUntilOption>.Instance.Name);

        var description = parseResult.GetValue(Opt<TaskDescriptionOption>.Instance) ?? "";
        var assignee = parseResult.GetValue(Opt<TaskAssigneeOption>.Instance);
        var estimate = parseResult.GetValue(Opt<TaskEstimateOption>.Instance);
        var labels = (parseResult.GetValue(Opt<TaskLabelOption>.Instance) ?? [])
            .Select(label => label.Trim().ToLowerInvariant())
            .Distinct()
            .ToArray();

        if (labels.Any(string.IsNullOrEmpty))
        {
            throw new ExitException("Labels must be non-empty.");
        }

        var dependsOn = parseResult.GetValue(Opt<TaskDependsOnOption>.Instance) ?? [];
        var parentId = parseResult.GetValue(Opt<TaskParentOption>.Instance);

        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariables);

        var dependencyRequests = dependsOn.Select(dependency =>
        {
            var separatorIndex = dependency.IndexOf(':');
            var dependencyType = separatorIndex < 0
                ? TaskDependencyTypes.Blocks
                : TaskDependencyTypes.Normalize(dependency[..separatorIndex]);
            var dependsOnId = separatorIndex < 0 ? dependency : dependency[(separatorIndex + 1)..];

            return new TaskDependencyRequest(dependsOnId, dependencyType);
        }).ToList();

        var result = await store.CreateTaskAsync(
            new TaskCreation
            {
                Title = title,
                Description = description,
                Priority = priority,
                Type = type,
                Assignee = assignee,
                EstimatedMinutes = estimate,
                DueAt = dueAt,
                DeferUntil = deferUntil,
                Labels = labels,
                DependsOn = dependencyRequests,
                ParentId = parentId,
                Actor = actor
            },
            cancellationToken);

        console.OkLine($"Created task '{result.Id}': {title.EscapeMarkup()}.");

        if (result.BlockedBy.Count > 0)
        {
            console.WriteLine($"  Blocked by: {string.Join(", ", result.BlockedBy)}");
        }

        return ExitCodes.Success;
    }
}
