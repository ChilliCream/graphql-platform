using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class ReopenTaskCommand : Command
{
    public ReopenTaskCommand() : base("reopen")
    {
        Description = "Reopen a closed task.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Options.Add(Opt<TaskReasonOption>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);

        this.AddExamples("task reopen \"app-1a2\"");

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

        var id = parseResult.GetRequiredValue(Opt<TaskIdArgument>.Instance);
        var reason = parseResult.GetValue(Opt<TaskReasonOption>.Instance) ?? "";
        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);
        var now = timeProvider.GetUtcNow();

        await using var connection = await store.ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var task = await store.GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        if (task.Status != TaskStates.Closed)
        {
            throw new ExitException($"Task '{id}' is not closed.");
        }

        await connection.ExecuteAsync(
            "UPDATE tasks SET status = @status, closed_at = NULL, "
            + "close_reason = @closeReason, updated_at = @updatedAt WHERE id = @id",
            new
            {
                status = TaskStates.Open,
                closeReason = "",
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
                Type = TaskEventTypes.Reopened,
                Actor = actor,
                OldValue = task.Status,
                NewValue = TaskStates.Open,
                Comment = string.IsNullOrEmpty(reason) ? null : reason,
                CreatedAt = now
            },
            cancellationToken,
            transaction);

        await transaction.CommitAsync(cancellationToken);

        console.OkLine($"Reopened task '{task.Id.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
