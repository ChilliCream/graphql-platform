using ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Label;

internal sealed class AddTaskLabelCommand : Command
{
    public AddTaskLabelCommand() : base("add")
    {
        Description = "Add one or more labels to a task.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Arguments.Add(Opt<LabelsArgument>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent tasks label add \"acme-1a2\" api parser");

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
        var labels = parseResult.GetRequiredValue(Opt<LabelsArgument>.Instance);
        var actor = await TaskActor.ResolveAsync(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), actorResolver, cancellationToken);

        var results = await store.AddLabelAsync(id, labels, actor, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new TaskLabelAddResult(id, results)));
            return ExitCodes.Success;
        }

        foreach (var result in results)
        {
            if (result.Added)
            {
                console.OkLine($"Added label '{result.Label.EscapeMarkup()}' to '{id.EscapeMarkup()}'.");
            }
            else
            {
                console.WriteLine($"Label '{result.Label}' is already on '{id}'.");
            }
        }

        return ExitCodes.Success;
    }

    public sealed record TaskLabelAddResult(string Id, IReadOnlyList<TaskLabelChange> Labels);
}
