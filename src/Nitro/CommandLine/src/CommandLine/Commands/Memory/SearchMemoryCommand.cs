using ChilliCream.Nitro.CommandLine.Commands.Memory.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Memory;

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

        // Collection band, curated first: `--tag`/`--type` narrow the
        // curated collection only, a journal entry has neither until it is
        // promoted.
        List<MemoryEntryResult> entries;

        try
        {
            entries = [];

            if (collection is MemoryCollections.Curated or MemoryCollections.All)
            {
                var curated = await store.SearchCuratedAsync(
                    query, scope, tags, type, since, limit, cancellationToken);
                entries.AddRange(curated.Select(MemoryEntryResult.FromCurated));
            }

            if (collection is MemoryCollections.Journal or MemoryCollections.All)
            {
                var journal = await store.SearchJournalAsync(query, scope, since, limit, cancellationToken);
                entries.AddRange(journal.Select(MemoryEntryResult.FromJournal));
            }
        }
        catch (MemoryScopeConflictException exception)
        {
            return MemoryScopeConflictReporting.Report(console, resultHolder, exception);
        }

        var results = limit is { } value ? entries.Take(value).ToList() : entries;

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<MemoryEntryResult>(results));
            return ExitCodes.Success;
        }

        if (results.Count == 0)
        {
            console.WriteLine("No memories found.");
            return ExitCodes.Success;
        }

        foreach (var entry in results)
        {
            MemoryEntryDisplay.WriteLine(console, entry);
        }

        return ExitCodes.Success;
    }
}
