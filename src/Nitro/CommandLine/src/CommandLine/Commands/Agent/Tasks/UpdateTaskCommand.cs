using ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks;

internal sealed class UpdateTaskCommand : Command
{
    public UpdateTaskCommand() : base("update")
    {
        Description = "Update one or more tasks' fields.";

        Arguments.Add(Opt<TaskIdsArgument>.Instance);
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
        Options.Add(Opt<TaskAddLabelOption>.Instance);
        Options.Add(Opt<TaskRemoveLabelOption>.Instance);
        Options.Add(Opt<TaskParentOption>.Instance);
        Options.Add(Opt<TaskClaimOption>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent tasks update \"app-1a2\" --status in_progress",
            "agent tasks update \"app-1a2\" --priority p1 --assignee alice",
            "agent tasks update \"app-1a2\" \"app-9z8\" --priority p1",
            "agent tasks update \"app-1a2\" --claim",
            "agent tasks update \"app-1a2\" --add-label api --remove-label triage",
            "agent tasks update \"app-1a2\" --parent \"app-9z8\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();
        var actorResolver = services.GetRequiredService<IActingActorResolver>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var ids = parseResult.GetRequiredValue(Opt<TaskIdsArgument>.Instance)
            .Distinct()
            .ToArray();
        var actor = await TaskActor.ResolveAsync(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), actorResolver, cancellationToken);

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
        var addLabelsGiven = parseResult.GetResult(Opt<TaskAddLabelOption>.Instance) is { Implicit: false };
        var removeLabelsGiven =
            parseResult.GetResult(Opt<TaskRemoveLabelOption>.Instance) is { Implicit: false };
        var parentGiven = parseResult.GetResult(Opt<TaskParentOption>.Instance) is { Implicit: false };
        var claimGiven = parseResult.GetResult(Opt<TaskClaimOption>.Instance) is { Implicit: false };

        if (!titleGiven && !descriptionGiven && !statusGiven && !priorityGiven && !typeGiven
            && !assigneeGiven && !notesGiven && !designGiven && !acceptanceCriteriaGiven
            && !dueGiven && !deferUntilGiven && !estimateGiven && !addLabelsGiven
            && !removeLabelsGiven && !parentGiven && !claimGiven)
        {
            throw new ExitException("Nothing to update. Pass at least one option.");
        }

        // --claim is shorthand for "--status in_progress --assignee <actor>";
        // an explicit --status or --assignee on the same call wins.
        var explicitStatusGiven = statusGiven;
        var explicitAssigneeGiven = assigneeGiven;

        if (claimGiven)
        {
            statusGiven = true;
            assigneeGiven = true;
        }

        string? title = null;

        if (titleGiven)
        {
            title = parseResult.GetValue(Opt<TaskTitleOption>.Instance) ?? "";

            if (title.Trim().Length is 0 || title.Length > 500)
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

        var addLabels = parseResult.GetValue(Opt<TaskAddLabelOption>.Instance) ?? [];
        var removeLabels = parseResult.GetValue(Opt<TaskRemoveLabelOption>.Instance) ?? [];
        var parentValue = parseResult.GetValue(Opt<TaskParentOption>.Instance);

        var coreFieldGiven = titleGiven || descriptionGiven || statusGiven || priorityGiven
            || typeGiven || assigneeGiven || notesGiven || designGiven || acceptanceCriteriaGiven
            || dueGiven || deferUntilGiven || estimateGiven;

        // Every id is validated up front so a bad id fails before any write,
        // including the --add-label/--remove-label/--parent writes below,
        // which go through the existing per-id store methods rather than the
        // bulk-update transaction.
        foreach (var id in ids)
        {
            await store.GetRequiredTaskAsync(id, cancellationToken);
        }

        if (coreFieldGiven)
        {
            await store.UpdateTasksAsync(
                ids,
                new TaskUpdate
                {
                    Actor = actor,
                    Title = title,
                    TitleGiven = titleGiven,
                    Description = parseResult.GetValue(Opt<TaskDescriptionOption>.Instance),
                    DescriptionGiven = descriptionGiven,
                    Status = statusGiven
                        ? (explicitStatusGiven
                            ? TaskStates.Normalize(parseResult.GetValue(Opt<TaskStatusOption>.Instance) ?? "")
                            : TaskStates.InProgress)
                        : null,
                    StatusGiven = statusGiven,
                    Priority = priority,
                    PriorityGiven = priorityGiven,
                    Type = typeGiven
                        ? TaskTypes.Normalize(parseResult.GetValue(Opt<TaskTypeOption>.Instance) ?? "")
                        : null,
                    TypeGiven = typeGiven,
                    Assignee = assigneeGiven
                        ? (explicitAssigneeGiven
                            ? parseResult.GetValue(Opt<TaskAssigneeOption>.Instance)
                            : actor)
                        : null,
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
        }

        foreach (var id in ids)
        {
            if (parentGiven)
            {
                await store.SetParentAsync(id, parentValue, actor, cancellationToken);
            }

            if (addLabelsGiven)
            {
                await store.AddLabelAsync(id, addLabels, actor, cancellationToken);
            }

            foreach (var label in removeLabels)
            {
                await store.RemoveLabelAsync(id, label, actor, cancellationToken);
            }
        }

        var tasks = new List<TaskItem>(ids.Length);

        foreach (var id in ids)
        {
            tasks.Add(await store.GetRequiredTaskAsync(id, cancellationToken));
        }

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(
                new ListResult<TaskSnapshotResult>(tasks.Select(task => TaskSnapshotResult.From(task)).ToArray()));

            return ExitCodes.Success;
        }

        foreach (var task in tasks)
        {
            console.OkLine($"Updated task '{task.Id.EscapeMarkup()}'.");
        }

        return ExitCodes.Success;
    }
}
