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

        // The journal collection is always empty until the journal capture
        // slice lands; `curated` and `all` both read the curated store.
        IReadOnlyList<MemoryRecord> records = collection == MemoryCollections.Journal
            ? []
            : await store.GetRecentCuratedAsync(limit, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(
                new ListResult<MemoryRecordResult>(records.Select(MemoryRecordResult.Create).ToArray()));

            return ExitCodes.Success;
        }

        if (records.Count == 0)
        {
            console.WriteLine("No memories found.");
            return ExitCodes.Success;
        }

        foreach (var record in records)
        {
            var tagsSuffix = record.Tags.Count > 0 ? $"  [{string.Join(", ", record.Tags)}]" : "";
            console.WriteLine(
                $"{record.Id}  {record.Type}  {MemoryDates.Format(record.UpdatedAt)}{tagsSuffix}");
        }

        return ExitCodes.Success;
    }
}
