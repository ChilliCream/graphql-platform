using ChilliCream.Nitro.CommandLine.Commands.Agent.Memory.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory;

internal sealed class ReindexMemoryCommand : Command
{
    public ReindexMemoryCommand() : base("reindex")
    {
        Description = "Rebuild the memory search index. Repair only: normal reads already self-heal.";

        Options.Add(Opt<MemoryReadScopeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent memory reindex", "agent memory reindex --scope global");

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
        var results = new List<MemoryIndexRebuildResult>();

        // Scope "all" reindexes every scope that has a store: the project
        // store only when one is found (mirroring `recent`'s scope "all"
        // convention), and always the global store. An explicit "project"
        // scope, in contrast, fails when no project workspace is found: the
        // caller named a target that does not exist.
        if (scope == MemoryScopes.Project || (scope == MemoryScopes.All && store.FindProjectWorkspaceDirectory() is not null))
        {
            results.Add(await store.RebuildIndexAsync(MemoryScopes.Project, cancellationToken));
        }

        if (scope is MemoryScopes.Global or MemoryScopes.All)
        {
            results.Add(await store.RebuildIndexAsync(MemoryScopes.Global, cancellationToken));
        }

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<MemoryIndexRebuildResult>(results));
            return ExitCodes.Success;
        }

        foreach (var result in results)
        {
            var noun = result.IndexedCount == 1 ? "memory" : "memories";
            console.WriteLine($"{result.Scope}: indexed {result.IndexedCount} {noun} ({result.IndexPath})");
        }

        return ExitCodes.Success;
    }
}
