using ChilliCream.Nitro.CommandLine.Commands.Agent.Memory.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory;

internal sealed class PromoteMemoryCommand : Command
{
    public PromoteMemoryCommand() : base("promote")
    {
        Description = "Promote a journal entry into a curated memory. Mechanical copy only: "
            + "no summarization or heuristics.";

        Arguments.Add(Opt<MemoryJournalIdArgument>.Instance);

        Options.Add(Opt<MemoryTypeOption>.Instance);
        Options.Add(Opt<MemoryTagOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        Validators.Add(result =>
        {
            var idGiven = result.GetResult(Opt<MemoryJournalIdArgument>.Instance) is { Implicit: false };
            var typeGiven = result.GetResult(Opt<MemoryTypeOption>.Instance) is { Implicit: false };
            var tagGiven = result.GetResult(Opt<MemoryTagOption>.Instance) is { Implicit: false };

            if (idGiven && !typeGiven)
            {
                result.AddError("Option '--type' is required when promoting a journal entry.");
            }

            if (!idGiven && (typeGiven || tagGiven))
            {
                result.AddError("'--type' and '--tag' require a journal entry id.");
            }
        });

        this.AddExamples(
            "agent memory promote",
            "agent memory promote \"01hqzxk8xdtd3fk3f0z7c5g8vm\" --type decision --tag ops");

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

        var id = parseResult.GetValue(Opt<MemoryJournalIdArgument>.Instance);

        if (id is null)
        {
            return await ListUnpromotedAsync(console, store, resultHolder, cancellationToken);
        }

        var type = parseResult.GetRequiredValue(Opt<MemoryTypeOption>.Instance);
        var tags = parseResult.GetValue(Opt<MemoryTagOption>.Instance) ?? [];

        MemoryPromotionOutcome outcome;

        outcome = await store.PromoteAsync(id, type, tags, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(MemoryPromotionResult.Create(outcome)));
            return ExitCodes.Success;
        }

        if (outcome.AlreadyPromoted)
        {
            console.WriteLine($"Journal entry '{id.EscapeMarkup()}' was already promoted as '"
                + $"{outcome.Record.Id.EscapeMarkup()}'.");
        }
        else
        {
            console.OkLine($"Promoted memory '{outcome.Record.Id.EscapeMarkup()}'.");
        }

        return ExitCodes.Success;
    }

    private static async Task<int> ListUnpromotedAsync(
        INitroConsole console,
        IMemoryStore store,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<MemoryJournalEntry> candidates;

        candidates = await store.GetUnpromotedJournalEntriesAsync(cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(
                new ListResult<MemoryJournalEntryResult>(
                    candidates.Select(MemoryJournalEntryResult.Create).ToArray()));

            return ExitCodes.Success;
        }

        if (candidates.Count == 0)
        {
            console.WriteLine("No unpromoted journal entries found.");
            return ExitCodes.Success;
        }

        foreach (var candidate in candidates)
        {
            console.WriteLine($"{candidate.Id}  {MemoryDates.Format(candidate.CreatedAt)}");
        }

        return ExitCodes.Success;
    }
}
