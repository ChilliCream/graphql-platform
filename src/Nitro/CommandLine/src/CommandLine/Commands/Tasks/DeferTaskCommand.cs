using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class DeferTaskCommand : Command
{
    public DeferTaskCommand() : base("defer")
    {
        Description = "Defer a task until a future date.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Options.Add(Opt<TaskUntilOption>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("task defer \"acme-1a2\" --until \"2026-02-01\"");

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
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var id = parseResult.GetRequiredValue(Opt<TaskIdArgument>.Instance);
        var untilValue = parseResult.GetRequiredValue(Opt<TaskUntilOption>.Instance);
        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);

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
