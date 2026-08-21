using ChilliCream.Nitro.CommandLine.Commands.Memory.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Memory;

internal sealed class TagsMemoryCommand : Command
{
    public TagsMemoryCommand() : base("tags")
    {
        Description = "List curated memory tags in use, with counts per scope.";

        Options.Add(Opt<MemoryReadScopeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent memory tags", "agent memory tags --scope global");

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

        var scope = parseResult.GetRequiredValue(Opt<MemoryReadScopeOption>.Instance);

        IReadOnlyList<MemoryRecord> records;

        try
        {
            records = await store.GetRecentCuratedAsync(scope, null, cancellationToken);
        }
        catch (MemoryScopeConflictException exception)
        {
            return MemoryScopeConflictReporting.Report(console, resultHolder, exception);
        }

        var rows = MemoryTagCounter.Count(records);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<MemoryTagCount>(rows));
            return ExitCodes.Success;
        }

        if (rows.Count == 0)
        {
            console.WriteLine("No tags.");
            return ExitCodes.Success;
        }

        foreach (var row in rows)
        {
            console.WriteLine(
                $"{row.Tag}  project={row.ProjectCount}  global={row.GlobalCount}  total={row.TotalCount}");
        }

        return ExitCodes.Success;
    }
}
