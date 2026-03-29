using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine.Commands.Workspaces;

internal sealed class ListWorkspaceCommand : Command
{
    public ListWorkspaceCommand(
        INitroConsole console,
        IWorkspacesClient client,
        IResultHolder resultHolder) : base("list")
    {
        Description = "Lists all workspaces";

        Options.Add(Opt<OptionalCursorOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IWorkspacesClient client,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        if (console.IsInteractive)
        {
            return await RenderInteractiveAsync(parseResult, console, client, resultHolder, ct);
        }

        return await RenderNonInteractiveAsync(parseResult, client, resultHolder, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        ParseResult parseResult,
        INitroConsole console,
        IWorkspacesClient client,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);
        var container = PaginationContainer
            .CreateConnectionData((after, first, token)
                => client.ListWorkspacesAsync(after ?? cursor, first, token))
            .PageSize(10);

        var workspace = await PagedTable
            .From(container)
            .Title("Workspaces")
            .AddColumn("Id", x => x.Id)
            .AddColumn("Name", x => x.Name)
            .AddColumn("IsPersonal", x => x.Personal.AsIcon())
            .RenderAsync(console, ct);

        if (workspace is not null)
        {
            resultHolder.SetResult(new ObjectResult(WorkspaceDetailPrompt.From(workspace).ToObject()));
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        ParseResult parseResult,
        IWorkspacesClient client,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);
        var data = await client.ListWorkspacesAsync(cursor, 10, ct);

        var items = data.Items
            .Select(WorkspaceDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        resultHolder.SetResult(
            new ObjectResult(new PaginatedListResult<WorkspaceDetailPrompt.WorkspaceDetailPromptResult>(items, data.EndCursor)));

        return ExitCodes.Success;
    }
}
