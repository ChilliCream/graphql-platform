using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class AddTaskDependencyCommand : Command
{
    public AddTaskDependencyCommand() : base("add")
    {
        Description = "Add a dependency between two tasks.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Arguments.Add(Opt<DependsOnIdArgument>.Instance);

        Options.Add(Opt<TaskDependencyTypeOption>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "task dep add \"acme-1a2\" \"acme-9z8\"",
            "task dep add \"acme-1a2\" \"acme-9z8\" --type waits-for");

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
        var dependsOnId = parseResult.GetRequiredValue(Opt<DependsOnIdArgument>.Instance);
        var typeValue = parseResult.GetValue(Opt<TaskDependencyTypeOption>.Instance);
        var type = typeValue is null
            ? TaskDependencyTypes.Blocks
            : TaskDependencyTypes.Normalize(typeValue);
        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);

        var result = await store.AddDependencyAsync(id, dependsOnId, type, actor, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new TaskDependencyAddedResult
            {
                Id = id,
                DependsOnId = dependsOnId,
                Type = type,
                Cycle = result.Cycle
            }));

            return ExitCodes.Success;
        }

        console.OkLine(
            $"Added {type.EscapeMarkup()} dependency: "
            + $"'{id.EscapeMarkup()}' -> '{dependsOnId.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }

    public sealed record TaskDependencyAddedResult
    {
        public required string Id { get; init; }
        public required string DependsOnId { get; init; }
        public required string Type { get; init; }
        public IReadOnlyList<string>? Cycle { get; init; }
    }
}
