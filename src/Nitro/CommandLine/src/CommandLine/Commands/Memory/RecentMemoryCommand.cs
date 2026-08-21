using ChilliCream.Nitro.CommandLine.Commands.Memory.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Memory;

internal sealed class RecentMemoryCommand : Command
{
    public RecentMemoryCommand() : base("recent")
    {
        Description = "List the most recently updated memories, most recent first.";

        Options.Add(Opt<MemoryCollectionOption>.Instance);
        Options.Add(Opt<MemoryLimitOption>.Instance);
        Options.Add(Opt<MemoryReadScopeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent memory recent",
            "agent memory recent --limit 5",
            "agent memory recent --collection all");

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

        var collection = parseResult.GetValue(Opt<MemoryCollectionOption>.Instance) ?? MemoryCollections.Curated;
        var limit = parseResult.GetValue(Opt<MemoryLimitOption>.Instance);
        var scope = parseResult.GetRequiredValue(Opt<MemoryReadScopeOption>.Instance);

        // Collection band, curated first: curated entries order by
        // `updated_at`, journal entries by `created_at`, since a journal
        // entry has no `updated_at`.
        List<MemoryEntryResult> entries;

        try
        {
            entries = [];

            if (collection is MemoryCollections.Curated or MemoryCollections.All)
            {
                var curated = await store.GetRecentCuratedAsync(scope, limit, cancellationToken);
                entries.AddRange(curated.Select(MemoryEntryResult.FromCurated));
            }

            if (collection is MemoryCollections.Journal or MemoryCollections.All)
            {
                var journal = await store.GetRecentJournalAsync(scope, limit, cancellationToken);
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
