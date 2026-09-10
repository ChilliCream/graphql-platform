using ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Epic;

internal sealed class CloseEligibleTaskEpicCommand : Command
{
    public CloseEligibleTaskEpicCommand() : base("close-eligible")
    {
        Description = "Close every epic whose children are all closed.";

        Options.Add(Opt<TaskActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent tasks epic close-eligible");

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

        var actor = await TaskActor.ResolveAsync(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), actorResolver, cancellationToken);

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
