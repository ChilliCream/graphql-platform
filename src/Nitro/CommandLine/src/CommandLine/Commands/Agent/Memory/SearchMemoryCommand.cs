using ChilliCream.Nitro.CommandLine.Commands.Agent.Memory.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory;

internal sealed class SearchMemoryCommand : Command
{
    public SearchMemoryCommand() : base("search")
    {
        Description = "Search curated memories by literal lexical match.";

        Arguments.Add(Opt<MemorySearchQueryArgument>.Instance);
        Options.Add(Opt<MemoryCollectionOption>.Instance);
        Options.Add(Opt<MemoryTagOption>.Instance);
        Options.Add(Opt<MemoryTypeOption>.Instance);
        Options.Add(Opt<MemorySinceOption>.Instance);
        Options.Add(Opt<MemoryReadScopeOption>.Instance);
        Options.Add(Opt<MemoryLimitOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent memory search \"deploy checklist\"",
            "agent memory search deploy --tag ops --type decision",
            "agent memory search deploy --since 2026-01-01T00:00:00Z");

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

        var query = parseResult.GetRequiredValue(Opt<MemorySearchQueryArgument>.Instance);
        var collection = parseResult.GetValue(Opt<MemoryCollectionOption>.Instance) ?? MemoryCollections.Curated;
        var tags = parseResult.GetValue(Opt<MemoryTagOption>.Instance) ?? [];
        var type = parseResult.GetValue(Opt<MemoryTypeOption>.Instance);
        var since = parseResult.GetValue(Opt<MemorySinceOption>.Instance);
        var scope = parseResult.GetRequiredValue(Opt<MemoryReadScopeOption>.Instance);
        var limit = parseResult.GetValue(Opt<MemoryLimitOption>.Instance);

        // Collection band, curated first: `--tag`/`--type` can never match a
        // journal entry, a journal entry has neither until it is promoted,
        // so a tag/type filter excludes the journal band entirely.
        var hasCuratedFilter = tags.Length > 0 || type is not null;

        // Both bands are queried under --collection all: split an explicit
        // limit across them (curated gets the ceiling half) instead of
        // handing each band the full limit and truncating the concatenation
        // afterward, which let curated results starve the journal band.
        var splitBands = collection is MemoryCollections.All && !hasCuratedFilter;
        int? curatedLimit = limit;
        int? journalLimit = limit;

        if (splitBands && limit is { } explicitLimit)
        {
            (curatedLimit, journalLimit) = MemoryBandLimit.Split(explicitLimit);
        }

        List<MemoryEntryResult> curated = [];
        List<MemoryEntryResult> journal = [];

        try
        {
            if (collection is MemoryCollections.Curated or MemoryCollections.All)
            {
                var curatedRecords = await store.SearchCuratedAsync(
                    query, scope, tags, type, since, curatedLimit, cancellationToken);
                curated = curatedRecords.Select(MemoryEntryResult.FromCurated).ToList();

                if (splitBands && limit is not null)
                {
                    // Unused remainder from a short curated band flows to
                    // the journal band.
                    journalLimit = MemoryBandLimit.GrowJournalWithCuratedShortfall(
                        curatedLimit!.Value, curatedRecords.Count, journalLimit!.Value);
                }
            }

            if (collection is MemoryCollections.Journal or MemoryCollections.All
                && !hasCuratedFilter)
            {
                var journalEntries = await store.SearchJournalAsync(
                    query, scope, since, journalLimit, cancellationToken);
                journal = journalEntries.Select(MemoryEntryResult.FromJournal).ToList();

                if (splitBands
                    && limit is { } journalExplicitLimit
                    && journalEntries.Count < journalLimit
                    && curated.Count == curatedLimit)
                {
                    // Unused remainder from a short journal band flows back
                    // to the curated band.
                    var curatedShare = MemoryBandLimit.GrowCuratedWithJournalShortfall(
                        journalExplicitLimit, journalEntries.Count);
                    var curatedRecords = await store.SearchCuratedAsync(
                        query, scope, tags, type, since, curatedShare, cancellationToken);
                    curated = curatedRecords.Select(MemoryEntryResult.FromCurated).ToList();
                }
            }
        }
        catch (MemoryScopeConflictException exception)
        {
            return MemoryScopeConflictReporting.Report(console, resultHolder, exception);
        }

        var entries = curated.Concat(journal).ToList();

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<MemoryEntryResult>(entries));
            return ExitCodes.Success;
        }

        if (entries.Count == 0)
        {
            console.WriteLine("No memories found.");
            return ExitCodes.Success;
        }

        foreach (var entry in entries)
        {
            MemoryEntryDisplay.WriteLine(console, entry);
        }

        return ExitCodes.Success;
    }
}
