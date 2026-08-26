using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class UndeferTaskCommand : Command
{
    public UndeferTaskCommand() : base("undefer")
    {
        Description = "Make a deferred task ready again.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent tasks undefer \"acme-1a2\"");

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
        var actor = await TaskActor.ResolveAsync(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), actorResolver, cancellationToken);

        var task = await store.UndeferTaskAsync(id, actor, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(TaskSnapshotResult.From(task)));
            return ExitCodes.Success;
        }

        console.OkLine($"Undeferred task '{task.Id.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
