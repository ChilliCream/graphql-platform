using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class UndeferTaskCommand : Command
{
    public UndeferTaskCommand() : base("undefer")
    {
        Description = "Make a deferred task ready again.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);

        this.AddExamples("task undefer \"acme-1a2\"");

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

        var id = parseResult.GetRequiredValue(Opt<TaskIdArgument>.Instance);
        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);

        var task = await store.UndeferTaskAsync(id, actor, cancellationToken);

        console.OkLine($"Undeferred task '{task.Id.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
