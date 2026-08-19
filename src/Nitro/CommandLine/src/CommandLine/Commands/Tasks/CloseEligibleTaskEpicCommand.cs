using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class CloseEligibleTaskEpicCommand : Command
{
    public CloseEligibleTaskEpicCommand() : base("close-eligible")
    {
        Description = "Close every epic whose children are all closed.";

        Options.Add(Opt<TaskActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("task epic close-eligible");

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

        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);

        var eligible = await store.CloseEligibleEpicsAsync(actor, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<TaskEpicStatus>(eligible));
            return ExitCodes.Success;
        }

        if (eligible.Count == 0)
        {
            console.WriteLine("No eligible epics.");
            return ExitCodes.Success;
        }

        foreach (var epic in eligible)
        {
            console.OkLine($"Closed epic '{epic.Id.EscapeMarkup()}'.");
        }

        return ExitCodes.Success;
    }
}
