using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class ListTaskCommentCommand : Command
{
    public ListTaskCommentCommand() : base("list")
    {
        Description = "List a task's comments.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent tasks comment list \"acme-1a2\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var id = parseResult.GetRequiredValue(Opt<TaskIdArgument>.Instance);

        var task = await store.GetRequiredTaskAsync(id, cancellationToken);
        var comments = await store.GetCommentsAsync(task.Id, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<TaskComment>(comments));
            return ExitCodes.Success;
        }

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
