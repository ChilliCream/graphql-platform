using ChilliCream.Nitro.CommandLine.Commands.Memory.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Memory;

internal sealed class ForgetMemoryCommand : Command
{
    public ForgetMemoryCommand() : base("forget")
    {
        Description = "Permanently delete a curated memory. This is a hard delete: the markdown "
            + "file and its index entry are removed, with no tombstone. Git history is not erased, "
            + "so a merge or checkout can resurrect the deleted content; forget is therefore not a "
            + "privacy-erasure guarantee.";

        Arguments.Add(Opt<MemoryIdArgument>.Instance);
        Options.Add(Opt<OptionalForceOption>.Instance);
        Options.Add(Opt<MemoryWriteScopeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent memory forget \"01hqzxk8xdtd3fk3f0z7c5g8vm\"",
            "agent memory forget \"01hqzxk8xdtd3fk3f0z7c5g8vm\" --force");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<IMemoryStore>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var id = parseResult.GetRequiredValue(Opt<MemoryIdArgument>.Instance);
        var force = parseResult.GetValue(Opt<OptionalForceOption>.Instance);
        var scope = parseResult.GetRequiredValue(Opt<MemoryWriteScopeOption>.Instance);

        // Existence is checked up front, before the confirmation prompt, so
        // a nonexistent memory fails immediately instead of asking to
        // confirm it.
        await store.GetRequiredAsync(id, scope, cancellationToken);

        if (!force)
        {
            if (console.IsInteractive)
            {
                var confirmed = await console.ConfirmAsync(
                    $"Permanently delete memory '{id.EscapeMarkup()}'? "
                    + "Git history may still retain its content.",
                    cancellationToken);

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

        var record = await store.ForgetAsync(id, scope, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(MemoryRecordResult.Create(record)));
            return ExitCodes.Success;
        }

        console.OkLine($"Deleted memory '{record.Id.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
