using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class UndeferTaskCommand : Command
{
    public UndeferTaskCommand() : base("undefer")
    {
        Description = "Make a deferred task ready again.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);

        this.AddExamples("task undefer \"acme-1a2\"");

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
        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);
        var now = timeProvider.GetUtcNow();

        await using var connection = await store.ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var task = await store.GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        if (task.Status != TaskStates.Deferred)
        {
            throw new ExitException($"Task '{id}' is not deferred.");
        }

        await connection.ExecuteAsync(
            "UPDATE tasks SET status = @status, defer_until = NULL, "
            + "updated_at = @updatedAt WHERE id = @id",
            new
            {
                status = TaskStates.Open,
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
                Type = TaskEventTypes.Undeferred,
                Actor = actor,
                OldValue = task.Status,
                NewValue = TaskStates.Open,
                CreatedAt = now
            },
            cancellationToken,
            transaction);

        await transaction.CommitAsync(cancellationToken);

        console.OkLine($"Undeferred task '{task.Id.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
