using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class AddTaskLabelCommand : Command
{
    public AddTaskLabelCommand() : base("add")
    {
        Description = "Add one or more labels to a task.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Arguments.Add(Opt<LabelsArgument>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);

        this.AddExamples("task label add \"acme-1a2\" api parser");

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
        var labels = parseResult.GetRequiredValue(Opt<LabelsArgument>.Instance);
        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);
        var now = timeProvider.GetUtcNow();

        await using var connection = await store.ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var task = await store.GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        var normalizedLabels = labels.Select(label => label.Trim().ToLowerInvariant()).ToList();

        if (normalizedLabels.Any(label => label.Length == 0))
        {
            throw new ExitException("Labels must be non-empty.");
        }

        var results = new List<(string Label, bool Added)>();

        foreach (var label in normalizedLabels)
        {
            var rowsAffected = await connection.ExecuteAsync(
                "INSERT OR IGNORE INTO labels (task_id, label) VALUES (@TaskId, @Label)",
                new { TaskId = task.Id, Label = label, cancellationToken },
                transaction);

            results.Add((label, rowsAffected > 0));
        }

        if (results.Any(result => result.Added))
        {
            await connection.ExecuteAsync(
                "UPDATE tasks SET updated_at = @updatedAt WHERE id = @id",
                new { updatedAt = now, id = task.Id, cancellationToken },
                transaction);
        }

        foreach (var (label, added) in results)
        {
            if (added)
            {
                await store.RecordEventAsync(
                    connection,
                    new TaskEvent
                    {
                        TaskId = task.Id,
                        Type = TaskEventTypes.LabelAdded,
                        Actor = actor,
                        NewValue = label,
                        CreatedAt = now
                    },
                    cancellationToken,
                    transaction);
            }
        }

        await transaction.CommitAsync(cancellationToken);

        foreach (var (label, added) in results)
        {
            if (added)
            {
                console.OkLine($"Added label '{label.EscapeMarkup()}' to '{task.Id.EscapeMarkup()}'.");
            }
            else
            {
                console.WriteLine($"Label '{label}' is already on '{task.Id}'.");
            }
        }

        return ExitCodes.Success;
    }
}
