using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

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
        var timeProvider = services.GetRequiredService<TimeProvider>();
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
        var now = timeProvider.GetUtcNow();
        var seed = $"{title}|{now:O}";

        await using var connection = await store.ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var resolvedDependencies = new List<(string TargetId, string Type, string Status)>();
        string id;

        if (parentId is not null)
        {
            var parent = await store.GetRequiredTaskAsync(
                connection, parentId, cancellationToken, transaction);
            resolvedDependencies.Add((parentId, TaskDependencyTypes.ParentChild, parent.Status));
            id = await store.CreateTaskIdAsync(connection, parentId, seed, cancellationToken, transaction);
        }
        else
        {
            id = await store.CreateTaskIdAsync(connection, null, seed, cancellationToken, transaction);
        }

        foreach (var dependency in dependsOn)
        {
            var separatorIndex = dependency.IndexOf(':');
            var dependencyType = separatorIndex < 0
                ? TaskDependencyTypes.Blocks
                : TaskDependencyTypes.Normalize(dependency[..separatorIndex]);
            var dependsOnId = separatorIndex < 0 ? dependency : dependency[(separatorIndex + 1)..];

            var target = await store.GetRequiredTaskAsync(
                connection, dependsOnId, cancellationToken, transaction);

            resolvedDependencies.Add((dependsOnId, dependencyType, target.Status));
        }

        var task = new TaskItem
        {
            Id = id,
            Title = title,
            Description = description,
            Status = TaskStates.Open,
            Priority = priority,
            Type = type,
            Assignee = assignee,
            EstimatedMinutes = estimate,
            DueAt = dueAt,
            DeferUntil = deferUntil,
            CreatedAt = now,
            CreatedBy = actor,
            UpdatedAt = now
        };

        await connection.ExecuteAsync(
            "INSERT INTO tasks (id, title, description, design, acceptance_criteria, notes, "
            + "status, priority, task_type, assignee, estimated_minutes, due_at, defer_until, "
            + "created_at, created_by, updated_at, closed_at, close_reason, deleted_at, "
            + "delete_reason) "
            + "VALUES (@Id, @Title, @Description, @Design, @AcceptanceCriteria, @Notes, "
            + "@Status, @Priority, @Type, @Assignee, @EstimatedMinutes, @DueAt, @DeferUntil, "
            + "@CreatedAt, @CreatedBy, @UpdatedAt, @ClosedAt, @CloseReason, @DeletedAt, "
            + "@DeleteReason)",
            task,
            transaction);

        foreach (var label in labels)
        {
            await connection.ExecuteAsync(
                "INSERT OR IGNORE INTO labels (task_id, label) VALUES (@TaskId, @Label)",
                new { TaskId = id, Label = label, cancellationToken },
                transaction);
        }

        foreach (var (targetId, dependencyType, _) in resolvedDependencies)
        {
            await connection.ExecuteAsync(
                "INSERT OR IGNORE INTO dependencies "
                + "(task_id, depends_on_id, dependency_type, created_at, created_by) "
                + "VALUES (@TaskId, @DependsOnId, @Type, @CreatedAt, @CreatedBy)",
                new TaskDependency
                {
                    TaskId = id,
                    DependsOnId = targetId,
                    Type = dependencyType,
                    CreatedAt = now,
                    CreatedBy = actor
                },
                transaction);
        }

        await store.RecordEventAsync(
            connection,
            new TaskEvent
            {
                TaskId = id,
                Type = TaskEventTypes.Created,
                Actor = actor,
                CreatedAt = now
            },
            cancellationToken,
            transaction);

        await transaction.CommitAsync(cancellationToken);

        console.OkLine($"Created task '{id}': {title.EscapeMarkup()}.");

        // A parent-child edge alone does not block the new task; only the
        // other blocking dependency types gate it (matching ComputeBlockedAsync).
        var blockedBy = resolvedDependencies
            .Where(d => d.Type != TaskDependencyTypes.ParentChild
                && TaskDependencyTypes.IsBlocking(d.Type)
                && !TaskStates.IsTerminal(d.Status))
            .Select(d => d.TargetId)
            .Distinct()
            .ToList();

        if (blockedBy.Count > 0)
        {
            console.WriteLine($"  Blocked by: {string.Join(", ", blockedBy)}");
        }

        return ExitCodes.Success;
    }
}
