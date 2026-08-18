using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

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
        var timeProvider = services.GetRequiredService<TimeProvider>();

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

        var now = timeProvider.GetUtcNow();

        await using var connection = await store.ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var task = await store.GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        var changedFields = new List<string>();
        string? oldStatus = null;
        string? newStatus = null;
        string? oldPriority = null;
        string? newPriority = null;
        string? oldAssignee = null;
        string? newAssignee = null;

        if (titleGiven)
        {
            var title = parseResult.GetValue(Opt<TaskTitleOption>.Instance) ?? "";

            if (title.Length is 0 or > 500)
            {
                throw new ExitException("The title must be 1-500 characters.");
            }

            if (title != task.Title)
            {
                task.Title = title;
                changedFields.Add("title");
            }
        }

        if (descriptionGiven)
        {
            var description = parseResult.GetValue(Opt<TaskDescriptionOption>.Instance) ?? "";

            if (description != task.Description)
            {
                task.Description = description;
                changedFields.Add("description");
            }
        }

        if (typeGiven)
        {
            var type = TaskTypes.Normalize(parseResult.GetValue(Opt<TaskTypeOption>.Instance) ?? "");

            if (type != task.Type)
            {
                task.Type = type;
                changedFields.Add("task_type");
            }
        }

        if (notesGiven)
        {
            var notes = parseResult.GetValue(Opt<TaskNotesOption>.Instance) ?? "";

            if (notes != task.Notes)
            {
                task.Notes = notes;
                changedFields.Add("notes");
            }
        }

        if (designGiven)
        {
            var design = parseResult.GetValue(Opt<TaskDesignOption>.Instance) ?? "";

            if (design != task.Design)
            {
                task.Design = design;
                changedFields.Add("design");
            }
        }

        if (acceptanceCriteriaGiven)
        {
            var acceptanceCriteria =
                parseResult.GetValue(Opt<TaskAcceptanceCriteriaOption>.Instance) ?? "";

            if (acceptanceCriteria != task.AcceptanceCriteria)
            {
                task.AcceptanceCriteria = acceptanceCriteria;
                changedFields.Add("acceptance_criteria");
            }
        }

        if (dueGiven)
        {
            var due = TaskDates.Parse(
                parseResult.GetValue(Opt<TaskDueOption>.Instance) ?? "",
                Opt<TaskDueOption>.Instance.Name);

            if (due != task.DueAt)
            {
                task.DueAt = due;
                changedFields.Add("due_at");
            }
        }

        if (deferUntilGiven)
        {
            var deferUntil = TaskDates.Parse(
                parseResult.GetValue(Opt<TaskDeferUntilOption>.Instance) ?? "",
                Opt<TaskDeferUntilOption>.Instance.Name);

            if (deferUntil != task.DeferUntil)
            {
                task.DeferUntil = deferUntil;
                changedFields.Add("defer_until");
            }
        }

        if (estimateGiven)
        {
            var estimate = parseResult.GetValue(Opt<TaskEstimateOption>.Instance);

            if (estimate != task.EstimatedMinutes)
            {
                task.EstimatedMinutes = estimate;
                changedFields.Add("estimated_minutes");
            }
        }

        if (statusGiven)
        {
            var status = TaskStates.Normalize(parseResult.GetValue(Opt<TaskStatusOption>.Instance) ?? "");

            if (status == TaskStates.Closed)
            {
                throw new ExitException("Use `nitro task close` to close a task.");
            }

            if (status == TaskStates.Tombstone)
            {
                throw new ExitException("Use `nitro task delete` to delete a task.");
            }

            if (task.Status == TaskStates.Closed)
            {
                throw new ExitException("Use `nitro task reopen` to reopen a task.");
            }

            if (status != task.Status)
            {
                oldStatus = task.Status;
                newStatus = status;
                task.Status = status;
            }
        }

        if (priorityGiven)
        {
            var priority =
                TaskPriorities.Parse(parseResult.GetValue(Opt<TaskPriorityOption>.Instance) ?? "");

            if (priority != task.Priority)
            {
                oldPriority = TaskPriorities.Format(task.Priority);
                newPriority = TaskPriorities.Format(priority);
                task.Priority = priority;
            }
        }

        if (assigneeGiven)
        {
            var assigneeValue = parseResult.GetValue(Opt<TaskAssigneeOption>.Instance);
            var assignee = string.IsNullOrEmpty(assigneeValue) ? null : assigneeValue;

            if (assignee != task.Assignee)
            {
                oldAssignee = task.Assignee ?? "";
                newAssignee = assignee ?? "";
                task.Assignee = assignee;
            }
        }

        task.UpdatedAt = now;

        await connection.ExecuteAsync(
            """
            UPDATE tasks
            SET title = @Title,
                description = @Description,
                design = @Design,
                acceptance_criteria = @AcceptanceCriteria,
                notes = @Notes,
                status = @Status,
                priority = @Priority,
                task_type = @Type,
                assignee = @Assignee,
                estimated_minutes = @EstimatedMinutes,
                due_at = @DueAt,
                defer_until = @DeferUntil,
                updated_at = @UpdatedAt
            WHERE id = @Id
            """,
            new
            {
                task.Title,
                task.Description,
                task.Design,
                task.AcceptanceCriteria,
                task.Notes,
                task.Status,
                task.Priority,
                task.Type,
                task.Assignee,
                task.EstimatedMinutes,
                task.DueAt,
                task.DeferUntil,
                task.UpdatedAt,
                Id = task.Id,
                cancellationToken
            },
            transaction);

        if (oldStatus is not null)
        {
            await store.RecordEventAsync(
                connection,
                new TaskEvent
                {
                    TaskId = id,
                    Type = TaskEventTypes.StatusChanged,
                    Actor = actor,
                    OldValue = oldStatus,
                    NewValue = newStatus,
                    CreatedAt = now
                },
                cancellationToken,
                transaction);
        }

        if (oldPriority is not null)
        {
            await store.RecordEventAsync(
                connection,
                new TaskEvent
                {
                    TaskId = id,
                    Type = TaskEventTypes.PriorityChanged,
                    Actor = actor,
                    OldValue = oldPriority,
                    NewValue = newPriority,
                    CreatedAt = now
                },
                cancellationToken,
                transaction);
        }

        if (oldAssignee is not null)
        {
            await store.RecordEventAsync(
                connection,
                new TaskEvent
                {
                    TaskId = id,
                    Type = TaskEventTypes.AssigneeChanged,
                    Actor = actor,
                    OldValue = oldAssignee,
                    NewValue = newAssignee,
                    CreatedAt = now
                },
                cancellationToken,
                transaction);
        }

        if (changedFields.Count > 0)
        {
            await store.RecordEventAsync(
                connection,
                new TaskEvent
                {
                    TaskId = id,
                    Type = TaskEventTypes.Updated,
                    Actor = actor,
                    Comment = string.Join(", ", changedFields),
                    CreatedAt = now
                },
                cancellationToken,
                transaction);
        }

        await transaction.CommitAsync(cancellationToken);

        console.OkLine($"Updated task '{id.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
