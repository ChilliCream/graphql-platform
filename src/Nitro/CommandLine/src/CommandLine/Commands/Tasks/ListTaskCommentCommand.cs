using System.Globalization;
using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class ListTaskCommentCommand : Command
{
    public ListTaskCommentCommand() : base("list")
    {
        Description = "List a task's comments.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);

        this.AddExamples("task comment list \"acme-1a2\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();

        var id = parseResult.GetRequiredValue(Opt<TaskIdArgument>.Instance);

        await using var connection = await store.ConnectAsync(cancellationToken);
        var task = await store.GetRequiredTaskAsync(connection, id, cancellationToken);

        // The intercepted read path cannot convert the TEXT-stored timestamp
        // column to DateTimeOffset, so this materializes an all-primitives
        // row and parses the timestamp itself.
        var comments = (await connection.QueryAsync<TaskCommentRow>(
                $"SELECT {TaskComment.Columns} FROM comments WHERE task_id = @id "
                + "ORDER BY created_at, id",
                new { id = task.Id }))
            .Select(r => r.ToTaskComment())
            .ToList();

        if (comments.Count == 0)
        {
            console.WriteLine("No comments.");
            return ExitCodes.Success;
        }

        for (var i = 0; i < comments.Count; i++)
        {
            if (i > 0)
            {
                console.WriteLine();
            }

            var comment = comments[i];

            console.WriteLine($"  [{i + 1}] {comment.Author} {TaskDates.Format(comment.CreatedAt)}");

            foreach (var line in comment.Text.ReplaceLineEndings("\n").Split('\n'))
            {
                console.WriteLine("    " + line);
            }
        }

        return ExitCodes.Success;
    }
}
