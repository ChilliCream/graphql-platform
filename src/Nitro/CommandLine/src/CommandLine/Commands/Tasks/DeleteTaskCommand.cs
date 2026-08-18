using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class DeleteTaskCommand : Command
{
    public DeleteTaskCommand() : base("delete")
    {
        Description = "Delete a task.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Options.Add(Opt<TaskReasonOption>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);
        Options.Add(Opt<OptionalForceOption>.Instance);

        this.AddExamples(
            "task delete \"app-1a2\"",
            "task delete \"app-1a2\" --force");

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
        var force = parseResult.GetValue(Opt<OptionalForceOption>.Instance);
        var now = timeProvider.GetUtcNow();

        await using var connection = await store.ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var task = await store.GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        if (!force)
        {
            if (console.IsInteractive)
            {
                var confirmed = await console.ConfirmAsync(
                    $"Delete task '{id.EscapeMarkup()}'?", cancellationToken);

                if (!confirmed)
                {
                    console.WriteLine("Aborted.");
                    return ExitCodes.Success;
                }
            }
            else
            {
                throw new ExitException("Use --force to delete without confirmation.");
            }
        }

        await connection.ExecuteAsync(
            "UPDATE tasks SET status = @status, deleted_at = @deletedAt, "
            + "delete_reason = @deleteReason, updated_at = @updatedAt WHERE id = @id",
            new
            {
                status = TaskStates.Tombstone,
                deletedAt = now,
                deleteReason = reason,
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
                Type = TaskEventTypes.Deleted,
                Actor = actor,
                OldValue = task.Status,
                NewValue = TaskStates.Tombstone,
                Comment = string.IsNullOrEmpty(reason) ? null : reason,
                CreatedAt = now
            },
            cancellationToken,
            transaction);

        await transaction.CommitAsync(cancellationToken);

        console.OkLine($"Deleted task '{task.Id.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
