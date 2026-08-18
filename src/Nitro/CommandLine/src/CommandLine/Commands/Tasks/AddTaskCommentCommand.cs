using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class AddTaskCommentCommand : Command
{
    public AddTaskCommentCommand() : base("add")
    {
        Description = "Add a comment to a task.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Arguments.Add(Opt<CommentTextArgument>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);

        this.AddExamples("task comment add \"acme-1a2\" \"Looks good to me.\"");

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
        var text = parseResult.GetRequiredValue(Opt<CommentTextArgument>.Instance);
        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);
        var now = timeProvider.GetUtcNow();

        await using var connection = await store.ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var task = await store.GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ExitException("The comment text must not be empty.");
        }

        await connection.ExecuteAsync(
            "INSERT INTO comments (task_id, author, text, created_at) "
            + "VALUES (@TaskId, @Author, @Text, @CreatedAt)",
            new { TaskId = task.Id, Author = actor, Text = text, CreatedAt = now, cancellationToken },
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
                Type = TaskEventTypes.Commented,
                Actor = actor,
                CreatedAt = now
            },
            cancellationToken,
            transaction);

        await transaction.CommitAsync(cancellationToken);

        console.OkLine($"Added comment to '{task.Id.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
