using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

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
        var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();

        var id = parseResult.GetRequiredValue(Opt<TaskIdArgument>.Instance);
        var text = parseResult.GetRequiredValue(Opt<CommentTextArgument>.Instance);
        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);

        var comment = await store.AddCommentAsync(id, text, actor, cancellationToken);

        console.OkLine($"Added comment to '{comment.TaskId.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
