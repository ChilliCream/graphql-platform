using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class UpdateTaskCommand : Command
{
    public UpdateTaskCommand() : base("update")
    {
        Description = "Update a task's fields.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Options.Add(Opt<TaskTitleOption>.Instance);
        Options.Add(Opt<TaskDescriptionOption>.Instance);
        Options.Add(Opt<TaskStatusOption>.Instance);
        Options.Add(Opt<TaskPriorityOption>.Instance);
        Options.Add(Opt<TaskTypeOption>.Instance);
        Options.Add(Opt<TaskAssigneeOption>.Instance);
        Options.Add(Opt<TaskNotesOption>.Instance);
        Options.Add(Opt<TaskDesignOption>.Instance);
        Options.Add(Opt<TaskAcceptanceCriteriaOption>.Instance);
        Options.Add(Opt<TaskDueOption>.Instance);
        Options.Add(Opt<TaskDeferUntilOption>.Instance);
        Options.Add(Opt<TaskEstimateOption>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "task update \"app-1a2\" --status in_progress",
            "task update \"app-1a2\" --priority p1 --assignee alice");

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

        var id = parseResult.GetRequiredValue(Opt<TaskIdArgument>.Instance);
        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariables);

        var titleGiven = parseResult.GetResult(Opt<TaskTitleOption>.Instance) is { Implicit: false };
        var descriptionGiven =
            parseResult.GetResult(Opt<TaskDescriptionOption>.Instance) is { Implicit: false };
        var statusGiven = parseResult.GetResult(Opt<TaskStatusOption>.Instance) is { Implicit: false };
        var priorityGiven = parseResult.GetResult(Opt<TaskPriorityOption>.Instance) is { Implicit: false };
        var typeGiven = parseResult.GetResult(Opt<TaskTypeOption>.Instance) is { Implicit: false };
        var assigneeGiven = parseResult.GetResult(Opt<TaskAssigneeOption>.Instance) is { Implicit: false };
        var notesGiven = parseResult.GetResult(Opt<TaskNotesOption>.Instance) is { Implicit: false };
        var designGiven = parseResult.GetResult(Opt<TaskDesignOption>.Instance) is { Implicit: false };
        var acceptanceCriteriaGiven =
            parseResult.GetResult(Opt<TaskAcceptanceCriteriaOption>.Instance) is { Implicit: false };
        var dueGiven = parseResult.GetResult(Opt<TaskDueOption>.Instance) is { Implicit: false };
        var deferUntilGiven =
            parseResult.GetResult(Opt<TaskDeferUntilOption>.Instance) is { Implicit: false };
        var estimateGiven = parseResult.GetResult(Opt<TaskEstimateOption>.Instance) is { Implicit: false };

        if (!titleGiven && !descriptionGiven && !statusGiven && !priorityGiven && !typeGiven
            && !assigneeGiven && !notesGiven && !designGiven && !acceptanceCriteriaGiven
            && !dueGiven && !deferUntilGiven && !estimateGiven)
        {
            throw new ExitException("Nothing to update. Pass at least one option.");
        }

        string? title = null;

        if (titleGiven)
        {
            title = parseResult.GetValue(Opt<TaskTitleOption>.Instance) ?? "";

            if (title.Length is 0 or > 500)
            {
                throw new ExitException("The title must be 1-500 characters.");
            }
        }

        DateTimeOffset? due = null;

        if (dueGiven)
        {
            due = TaskDates.Parse(
                parseResult.GetValue(Opt<TaskDueOption>.Instance) ?? "",
                Opt<TaskDueOption>.Instance.Name);
        }

        DateTimeOffset? deferUntil = null;

        if (deferUntilGiven)
        {
            deferUntil = TaskDates.Parse(
                parseResult.GetValue(Opt<TaskDeferUntilOption>.Instance) ?? "",
                Opt<TaskDeferUntilOption>.Instance.Name);
        }

        int? priority = null;

        if (priorityGiven)
        {
            priority = TaskPriorities.Parse(parseResult.GetValue(Opt<TaskPriorityOption>.Instance) ?? "");
        }

        await store.UpdateTaskAsync(
            id,
            new TaskUpdate
            {
                Actor = actor,
                Title = title,
                TitleGiven = titleGiven,
                Description = parseResult.GetValue(Opt<TaskDescriptionOption>.Instance),
                DescriptionGiven = descriptionGiven,
                Status = statusGiven
                    ? TaskStates.Normalize(parseResult.GetValue(Opt<TaskStatusOption>.Instance) ?? "")
                    : null,
                StatusGiven = statusGiven,
                Priority = priority,
                PriorityGiven = priorityGiven,
                Type = typeGiven
                    ? TaskTypes.Normalize(parseResult.GetValue(Opt<TaskTypeOption>.Instance) ?? "")
                    : null,
                TypeGiven = typeGiven,
                Assignee = parseResult.GetValue(Opt<TaskAssigneeOption>.Instance),
                AssigneeGiven = assigneeGiven,
                Notes = parseResult.GetValue(Opt<TaskNotesOption>.Instance),
                NotesGiven = notesGiven,
                Design = parseResult.GetValue(Opt<TaskDesignOption>.Instance),
                DesignGiven = designGiven,
                AcceptanceCriteria = parseResult.GetValue(Opt<TaskAcceptanceCriteriaOption>.Instance),
                AcceptanceCriteriaGiven = acceptanceCriteriaGiven,
                DueAt = due,
                DueAtGiven = dueGiven,
                DeferUntil = deferUntil,
                DeferUntilGiven = deferUntilGiven,
                EstimatedMinutes = parseResult.GetValue(Opt<TaskEstimateOption>.Instance),
                EstimatedMinutesGiven = estimateGiven
            },
            cancellationToken);

        if (!console.IsHumanReadable)
        {
            var updatedTask = await store.GetRequiredTaskAsync(id, cancellationToken);

            resultHolder.SetResult(new ObjectResult(TaskSnapshotResult.From(updatedTask)));

            return ExitCodes.Success;
        }

        console.OkLine($"Updated task '{id.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
