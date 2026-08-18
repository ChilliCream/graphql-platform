using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class DeferTaskCommand : Command
{
    public DeferTaskCommand() : base("defer")
    {
        Description = "Defer a task until a future date.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Options.Add(Opt<TaskUntilOption>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);

        this.AddExamples("task defer \"acme-1a2\" --until \"2026-02-01\"");

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
        var untilValue = parseResult.GetRequiredValue(Opt<TaskUntilOption>.Instance);
        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);
        var now = timeProvider.GetUtcNow();

        var deferUntil = TaskDates.Parse(untilValue, Opt<TaskUntilOption>.Instance.Name);

        await using var connection = await store.ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var task = await store.GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        if (task.Status is not (TaskStates.Open or TaskStates.InProgress))
        {
            throw new ExitException("Only open or in-progress tasks can be deferred.");
        }

        await connection.ExecuteAsync(
            "UPDATE tasks SET status = @status, defer_until = @deferUntil, "
            + "updated_at = @updatedAt WHERE id = @id",
            new
            {
                status = TaskStates.Deferred,
                deferUntil,
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
                Type = TaskEventTypes.Deferred,
                Actor = actor,
                OldValue = task.Status,
                NewValue = TaskDates.Format(deferUntil),
                CreatedAt = now
            },
            cancellationToken,
            transaction);

        await transaction.CommitAsync(cancellationToken);

        console.OkLine(
            $"Deferred task '{task.Id.EscapeMarkup()}' until {TaskDates.Format(deferUntil).EscapeMarkup()}.");

        return ExitCodes.Success;
    }
}
