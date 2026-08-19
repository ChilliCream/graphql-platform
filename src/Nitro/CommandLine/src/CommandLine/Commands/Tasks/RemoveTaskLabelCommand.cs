using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class RemoveTaskLabelCommand : Command
{
    public RemoveTaskLabelCommand() : base("remove")
    {
        Description = "Remove a label from a task.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Arguments.Add(Opt<LabelArgument>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("task label remove \"acme-1a2\" api");

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
        var label = parseResult.GetRequiredValue(Opt<LabelArgument>.Instance).Trim().ToLowerInvariant();
        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);

        await store.RemoveLabelAsync(id, label, actor, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new TaskLabelRemovedResult(id, label)));
            return ExitCodes.Success;
        }

        console.OkLine($"Removed label '{label.EscapeMarkup()}' from '{id.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }

    public sealed record TaskLabelRemovedResult(string Id, string Label);
}
