using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class AddTaskCommentCommand : Command
{
    public AddTaskCommentCommand() : base("add")
    {
        Description = "Add a comment to a task.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Arguments.Add(Opt<CommentTextArgument>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent tasks comment add \"acme-1a2\" \"Looks good to me.\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();
        var actorResolver = services.GetRequiredService<IActingActorResolver>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var id = parseResult.GetRequiredValue(Opt<TaskIdArgument>.Instance);
        var text = parseResult.GetRequiredValue(Opt<CommentTextArgument>.Instance);
        var actor = await TaskActor.ResolveAsync(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), actorResolver, cancellationToken);

        var comment = await store.AddCommentAsync(id, text, actor, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(comment));
            return ExitCodes.Success;
        }

        console.OkLine($"Added comment to '{comment.TaskId.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
