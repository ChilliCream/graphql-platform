using ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks;

internal sealed class RemoveTaskDependencyCommand : Command
{
    public RemoveTaskDependencyCommand() : base("remove")
    {
        Description = "Remove a dependency between two tasks.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Arguments.Add(Opt<DependsOnIdArgument>.Instance);

        Options.Add(Opt<TaskActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent tasks dep remove \"acme-1a2\" \"acme-9z8\"");

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
        var dependsOnId = parseResult.GetRequiredValue(Opt<DependsOnIdArgument>.Instance);
        var actor = await TaskActor.ResolveAsync(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), actorResolver, cancellationToken);

        await store.RemoveDependencyAsync(id, dependsOnId, actor, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new TaskDependencyRemovedResult(id, dependsOnId)));
            return ExitCodes.Success;
        }

        console.OkLine(
            $"Removed dependency: '{id.EscapeMarkup()}' -> '{dependsOnId.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }

    public sealed record TaskDependencyRemovedResult(string Id, string DependsOnId);
}
