using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class CloseTaskCommand : Command
{
    public CloseTaskCommand() : base("close")
    {
        Description = "Close one or more tasks.";

        Arguments.Add(Opt<TaskIdsArgument>.Instance);
        Options.Add(Opt<TaskReasonOption>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);

        this.AddExamples(
            "task close \"app-1a2\"",
            "task close \"app-1a2\" \"app-9z8\" --reason \"Fixed in v2\"");

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
        var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();

        var ids = parseResult.GetRequiredValue(Opt<TaskIdsArgument>.Instance)
            .Distinct()
            .ToArray();
        var reason = parseResult.GetValue(Opt<TaskReasonOption>.Instance) ?? "";
        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);
        var now = timeProvider.GetUtcNow();

        await using var connection = await store.ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Every task is loaded and validated before any write happens, which
        // gives close its all-or-nothing behavior: nothing is written until
        // every id has passed.
        var tasks = new List<TaskItem>();

        foreach (var id in ids)
        {
            var task = await store.GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

            if (task.Status == TaskStates.Closed)
            {
                throw new ExitException($"Task '{id}' is already closed.");
            }

            tasks.Add(task);
        }

        foreach (var task in tasks)
        {
            await connection.ExecuteAsync(
                "UPDATE tasks SET status = @status, closed_at = @closedAt, "
                + "close_reason = @closeReason, updated_at = @updatedAt WHERE id = @id",
                new
                {
                    status = TaskStates.Closed,
                    closedAt = now,
                    closeReason = reason,
                    updatedAt = now,
                    id = task.Id,
                    cancellationToken
                },
                transaction);

            await store.RecordEventAsync(
                connection,
                new TaskEvent
                {
                    TaskId = task.Id,
                    Type = TaskEventTypes.Closed,
                    Actor = actor,
                    OldValue = task.Status,
                    NewValue = TaskStates.Closed,
                    Comment = string.IsNullOrEmpty(reason) ? null : reason,
                    CreatedAt = now
                },
                cancellationToken,
                transaction);
        }

        await transaction.CommitAsync(cancellationToken);

        foreach (var task in tasks)
        {
            console.OkLine($"Closed task '{task.Id.EscapeMarkup()}'.");
        }

        return ExitCodes.Success;
    }
}
