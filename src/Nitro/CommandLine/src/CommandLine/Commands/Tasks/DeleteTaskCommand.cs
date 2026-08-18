using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class DeleteTaskCommand : Command
{
    public DeleteTaskCommand() : base("delete")
    {
        Description = "Delete a task.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Options.Add(Opt<TaskReasonOption>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);
        Options.Add(Opt<OptionalForceOption>.Instance);

        this.AddExamples(
            "task delete \"app-1a2\"",
            "task delete \"app-1a2\" --force");

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
        var reason = parseResult.GetValue(Opt<TaskReasonOption>.Instance) ?? "";
        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);
        var force = parseResult.GetValue(Opt<OptionalForceOption>.Instance);

        // Existence is checked up front, before the confirmation prompt, so a
        // nonexistent task fails immediately instead of asking to confirm it.
        await store.GetRequiredTaskAsync(id, cancellationToken);

        if (!force)
        {
            if (console.IsInteractive)
            {
                var confirmed = await console.ConfirmAsync(
                    $"Delete task '{id.EscapeMarkup()}'?", cancellationToken);

                if (!confirmed)
                {
                    console.WriteLine("Aborted.");
                    return ExitCodes.Success;
                }
            }
            else
            {
                throw new ExitException("Use --force to delete without confirmation.");
            }
        }

        var task = await store.DeleteTaskAsync(id, reason, actor, cancellationToken);

        console.OkLine($"Deleted task '{task.Id.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
