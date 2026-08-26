using ChilliCream.Nitro.CommandLine.Commands.Agent.Memory.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory;

internal sealed class WhereMemoryCommand : Command
{
    public WhereMemoryCommand() : base("where")
    {
        Description = "Print the resolved project and global memory store paths.";

        Options.Add(Opt<MemoryReadScopeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent memory where",
            "agent memory where --scope global");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<IMemoryStore>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var scope = parseResult.GetRequiredValue(Opt<MemoryReadScopeOption>.Instance);

        var locations = new List<MemoryLocationResult>();

        if (scope is MemoryScopes.Project or MemoryScopes.All)
        {
            var workspaceDirectory = store.FindProjectWorkspaceDirectory();

            locations.Add(new MemoryLocationResult(
                MemoryScopes.Project,
                workspaceDirectory is null ? null : AgentWorkspace.GetMemoryDirectory(workspaceDirectory)));
        }

        if (scope is MemoryScopes.Global or MemoryScopes.All)
        {
            locations.Add(new MemoryLocationResult(MemoryScopes.Global, store.GlobalMemoryDirectory));
        }

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<MemoryLocationResult>(locations));
            return Task.FromResult(ExitCodes.Success);
        }

        foreach (var location in locations)
        {
            console.WriteLine($"{location.Scope}: {location.Path ?? "(not found)"}");
        }

        return Task.FromResult(ExitCodes.Success);
    }

    public sealed record MemoryLocationResult(string Scope, string? Path);
}
