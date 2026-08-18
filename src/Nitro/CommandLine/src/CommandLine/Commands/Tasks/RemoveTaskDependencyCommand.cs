using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class RemoveTaskDependencyCommand : Command
{
    public RemoveTaskDependencyCommand() : base("remove")
    {
        Description = "Remove a dependency between two tasks.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Arguments.Add(Opt<DependsOnIdArgument>.Instance);

        Options.Add(Opt<TaskActorOption>.Instance);

        this.AddExamples("task dep remove \"acme-1a2\" \"acme-9z8\"");

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
        var dependsOnId = parseResult.GetRequiredValue(Opt<DependsOnIdArgument>.Instance);
        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);
        var now = timeProvider.GetUtcNow();

        await using var connection = await store.ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await store.GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        var existingType = await connection.QueryFirstOrDefaultAsync<string>(
            "SELECT dependency_type FROM dependencies "
            + "WHERE task_id = @id AND depends_on_id = @dependsOnId",
            new { id, dependsOnId, cancellationToken },
            transaction);

        if (existingType is null)
        {
            throw new ExitException("Dependency does not exist.");
        }

        await connection.ExecuteAsync(
            "DELETE FROM dependencies WHERE task_id = @id AND depends_on_id = @dependsOnId",
            new { id, dependsOnId, cancellationToken },
            transaction);

        await connection.ExecuteAsync(
            "UPDATE tasks SET updated_at = @updatedAt WHERE id = @id",
            new { updatedAt = now, id, cancellationToken },
            transaction);

        await store.RecordEventAsync(
            connection,
            new TaskEvent
            {
                TaskId = id,
                Type = TaskEventTypes.DependencyRemoved,
                Actor = actor,
                OldValue = null,
                NewValue = $"{existingType}:{dependsOnId}",
                CreatedAt = now
            },
            cancellationToken,
            transaction);

        await transaction.CommitAsync(cancellationToken);

        console.OkLine(
            $"Removed dependency: '{id.EscapeMarkup()}' -> '{dependsOnId.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
