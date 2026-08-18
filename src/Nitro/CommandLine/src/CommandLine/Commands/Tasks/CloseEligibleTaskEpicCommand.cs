using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class CloseEligibleTaskEpicCommand : Command
{
    private const string CloseReason = "All children are closed.";

    public CloseEligibleTaskEpicCommand() : base("close-eligible")
    {
        Description = "Close every epic whose children are all closed.";

        Options.Add(Opt<TaskActorOption>.Instance);

        this.AddExamples("task epic close-eligible");

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

        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);
        var now = timeProvider.GetUtcNow();

        await using var connection = await store.ConnectAsync(cancellationToken);

        var epics = await StatusTaskEpicCommand.GetEpicsAsync(connection, cancellationToken);
        var eligible = epics.Where(epic => epic.IsEligibleForClose).ToList();

        if (eligible.Count == 0)
        {
            console.WriteLine("No eligible epics.");
            return ExitCodes.Success;
        }

        // Every epic is validated up front (IsEligibleForClose, checked
        // against data read before the transaction opens); the update loop
        // below is then all-or-nothing, matching CloseTaskCommand's pattern.
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var epic in eligible)
        {
            await connection.ExecuteAsync(
                "UPDATE tasks SET status = @status, closed_at = @closedAt, "
                + "close_reason = @closeReason, updated_at = @updatedAt WHERE id = @id",
                new
                {
                    status = TaskStates.Closed,
                    closedAt = now,
                    closeReason = CloseReason,
                    updatedAt = now,
                    id = epic.Id,
                    cancellationToken
                },
                transaction);

            await store.RecordEventAsync(
                connection,
                new TaskEvent
                {
                    TaskId = epic.Id,
                    Type = TaskEventTypes.Closed,
                    Actor = actor,
                    OldValue = epic.Status,
                    NewValue = TaskStates.Closed,
                    Comment = CloseReason,
                    CreatedAt = now
                },
                cancellationToken,
                transaction);
        }

        await transaction.CommitAsync(cancellationToken);

        foreach (var epic in eligible)
        {
            console.OkLine($"Closed epic '{epic.Id.EscapeMarkup()}'.");
        }

        return ExitCodes.Success;
    }
}
