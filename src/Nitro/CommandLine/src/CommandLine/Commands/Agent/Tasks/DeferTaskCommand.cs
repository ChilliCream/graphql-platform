using ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks;

internal sealed class DeferTaskCommand : Command
{
    public DeferTaskCommand() : base("defer")
    {
        Description = "Defer a task until a future date.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Options.Add(Opt<TaskUntilOption>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent tasks defer \"acme-1a2\" --until \"2026-02-01\"");

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
        var untilValue = parseResult.GetRequiredValue(Opt<TaskUntilOption>.Instance);
        var actor = await TaskActor.ResolveAsync(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), actorResolver, cancellationToken);

        var deferUntil = TaskDates.Parse(untilValue, Opt<TaskUntilOption>.Instance.Name);

        var task = await store.DeferTaskAsync(id, deferUntil, actor, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(TaskSnapshotResult.From(task)));
            return ExitCodes.Success;
        }

        console.OkLine(
            $"Deferred task '{task.Id.EscapeMarkup()}' until {TaskDates.Format(deferUntil).EscapeMarkup()}.");

        return ExitCodes.Success;
    }
}
