using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class CloseTaskCommand : Command
{
    public CloseTaskCommand() : base("close")
    {
        Description = "Close one or more tasks.";

        Arguments.Add(Opt<TaskIdsArgument>.Instance);
        Options.Add(Opt<TaskReasonOption>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);

        this.AddExamples(
            "task close \"app-1a2\"",
            "task close \"app-1a2\" \"app-9z8\" --reason \"Fixed in v2\"");

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

        var ids = parseResult.GetRequiredValue(Opt<TaskIdsArgument>.Instance)
            .Distinct()
            .ToArray();
        var reason = parseResult.GetValue(Opt<TaskReasonOption>.Instance) ?? "";
        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);

        var tasks = await store.CloseTaskAsync(ids, reason, actor, cancellationToken);

        foreach (var task in tasks)
        {
            console.OkLine($"Closed task '{task.Id.EscapeMarkup()}'.");
        }

        return ExitCodes.Success;
    }
}
