using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine.Commands.Workspaces;

internal sealed class ListWorkspaceCommand : Command
{
    public ListWorkspaceCommand() : base("list")
    {
        Description = "List all workspaces.";

        Options.Add(Opt<OptionalCursorOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("workspace list");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IWorkspacesClient>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);

        if (console.IsInteractive)
        {
            return await RenderInteractiveAsync(cursor, console, client, resultHolder, ct);
        }

        return await RenderNonInteractiveAsync(cursor, client, resultHolder, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        string? cursor,
        INitroConsole console,
        IWorkspacesClient client,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
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
        string? cursor,
        IWorkspacesClient client,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        var data = await client.ListWorkspacesAsync(cursor, 10, ct);

        var items = data.Items
            .Select(WorkspaceDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        resultHolder.SetResult(
            new PaginatedListResult<WorkspaceDetailPrompt.WorkspaceDetailPromptResult>(items, data.EndCursor));

        return ExitCodes.Success;
    }
}
