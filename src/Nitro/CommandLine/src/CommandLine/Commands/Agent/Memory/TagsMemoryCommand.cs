using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory;

internal sealed class TagsMemoryCommand : Command
{
    public TagsMemoryCommand() : base("tags")
    {
        Description = "List curated memory tags in use, with counts per scope.";

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

        IReadOnlyList<MemoryRecord> records;

        records = await store.GetRecentCuratedAsync(null, cancellationToken);

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
                $"{row.Tag}  {row.Count}");
        }

        return ExitCodes.Success;
    }
}
