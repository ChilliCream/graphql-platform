using System.CommandLine.Invocation;
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
    public ListWorkspaceCommand() : base("list")
    {
        Description = "Lists all workspaces";

        AddOption(Opt<CursorOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IWorkspacesClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IWorkspacesClient client,
        CancellationToken ct)
    {
        if (console.IsHumanReadable())
        {
            return await RenderInteractiveAsync(context, console, client, ct);
        }

        return await RenderNonInteractiveAsync(context, client, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        InvocationContext context,
        IAnsiConsole console,
        IWorkspacesClient client,
        CancellationToken ct)
    {
        var container = PaginationContainer
            .CreateConnectionData(client.ListWorkspacesAsync)
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
            context.SetResult(WorkspaceDetailPrompt.From(workspace).ToObject());
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        InvocationContext context,
        IWorkspacesClient client,
        CancellationToken ct)
    {
        var cursor = context.ParseResult.GetValueForOption(Opt<CursorOption>.Instance);
        var data = await client.ListWorkspacesAsync(cursor, 10, ct);

        var items = data.Items
            .Select(WorkspaceDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        context.SetResult(new PaginatedListResult<WorkspaceDetailPrompt.WorkspaceDetailPromptResult>(items, data.EndCursor));

        return ExitCodes.Success;
    }
}
