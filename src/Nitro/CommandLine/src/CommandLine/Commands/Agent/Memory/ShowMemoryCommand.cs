using ChilliCream.Nitro.CommandLine.Commands.Agent.Memory.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory;

internal sealed class ShowMemoryCommand : Command
{
    public ShowMemoryCommand() : base("show")
    {
        Description = "Show a curated memory's details.";

        Arguments.Add(Opt<MemoryIdArgument>.Instance);
        Options.Add(Opt<MemoryReadScopeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent memory show \"01hqzxk8xdtd3fk3f0z7c5g8vm\"");

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
        var scope = parseResult.GetRequiredValue(Opt<MemoryReadScopeOption>.Instance);

        MemoryRecord record;

        try
        {
            record = await store.GetRequiredAsync(id, scope, cancellationToken);
        }
        catch (MemoryScopeConflictException exception)
        {
            return MemoryScopeConflictReporting.Report(console, resultHolder, exception);
        }

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(MemoryRecordDetailResult.Create(record)));
            return ExitCodes.Success;
        }

        console.WriteLine($"{record.Id} ({record.Type})");
        console.WriteLine($"Scope: {record.Scope}");

        if (record.Tags.Count > 0)
        {
            console.WriteLine($"Tags: {string.Join(", ", record.Tags)}");
        }

        console.WriteLine($"Created: {MemoryDates.Format(record.CreatedAt)} by {record.CreatedBy}");
        console.WriteLine($"Updated: {MemoryDates.Format(record.UpdatedAt)}");

        if (record.PromotedFrom is { } promotedFrom)
        {
            console.WriteLine($"Promoted from: {promotedFrom}");
        }

        console.WriteLine();
        console.WriteLine(record.Body);

        return ExitCodes.Success;
    }
}
