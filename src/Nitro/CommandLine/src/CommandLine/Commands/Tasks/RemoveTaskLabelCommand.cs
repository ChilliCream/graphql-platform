using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class RemoveTaskLabelCommand : Command
{
    public RemoveTaskLabelCommand() : base("remove")
    {
        Description = "Remove a label from a task.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Arguments.Add(Opt<LabelArgument>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);

        this.AddExamples("task label remove \"acme-1a2\" api");

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
        var label = parseResult.GetRequiredValue(Opt<LabelArgument>.Instance).Trim().ToLowerInvariant();
        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);
        var now = timeProvider.GetUtcNow();

        await using var connection = await store.ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var task = await store.GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        var exists = await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM labels WHERE task_id = @TaskId AND label = @Label",
            new { TaskId = task.Id, Label = label, cancellationToken },
            transaction);

        if (exists == 0)
        {
            throw new ExitException($"Label '{label}' is not on '{task.Id}'.");
        }

        await connection.ExecuteAsync(
            "DELETE FROM labels WHERE task_id = @TaskId AND label = @Label",
            new { TaskId = task.Id, Label = label, cancellationToken },
            transaction);

        await connection.ExecuteAsync(
            "UPDATE tasks SET updated_at = @updatedAt WHERE id = @id",
            new { updatedAt = now, id = task.Id, cancellationToken },
            transaction);

        await store.RecordEventAsync(
            connection,
            new TaskEvent
            {
                TaskId = task.Id,
                Type = TaskEventTypes.LabelRemoved,
                Actor = actor,
                OldValue = label,
                CreatedAt = now
            },
            cancellationToken,
            transaction);

        await transaction.CommitAsync(cancellationToken);

        console.OkLine($"Removed label '{label.EscapeMarkup()}' from '{task.Id.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
