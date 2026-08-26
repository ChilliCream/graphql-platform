using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

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
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent tasks delete \"app-1a2\"",
            "agent tasks delete \"app-1a2\" --force");

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
        var reason = parseResult.GetValue(Opt<TaskReasonOption>.Instance) ?? "";
        var actor = await TaskActor.ResolveAsync(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), actorResolver, cancellationToken);
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

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(TaskSnapshotResult.From(task)));
            return ExitCodes.Success;
        }

        console.OkLine($"Deleted task '{task.Id.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
