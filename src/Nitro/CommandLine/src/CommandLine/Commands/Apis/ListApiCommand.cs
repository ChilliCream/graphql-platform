using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis;

internal sealed class ListApiCommand : Command
{
    public ListApiCommand() : base("list")
    {
        Description = "Lists all APIs of a workspace";

        Options.Add(Opt<CursorOption>.Instance);
        Options.Add(Opt<WorkspaceIdOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IApisClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IApisClient client,
        CancellationToken ct)
    {
        var workspaceId = context.RequireWorkspaceId();

        if (console.IsInteractive)
        {
            return await RenderInteractiveAsync(context, console, client, workspaceId, ct);
        }

        return await RenderNonInteractiveAsync(context, client, workspaceId, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        InvocationContext context,
        INitroConsole console,
        IApisClient client,
        string workspaceId,
        CancellationToken ct)
    {
        var cursor = context.ParseResult.GetValueForOption(Opt<CursorOption>.Instance);
        var container = PaginationContainer
            .CreateConnectionData((after, first, token)
                => client.ListApisAsync(workspaceId, after ?? cursor, first, token))
            .PageSize(10);

        var api = await PagedTable
            .From(container)
            .Title("APIs")
            .AddColumn("Id", x => x.Id)
            .AddColumn("Name", x => x.Name)
            .AddColumn("Path", x => string.Join("/", x.Path))
            .RenderAsync(console, ct);

        if (api is not null)
        {
            context.SetResult(ApiDetailPrompt.From(api).ToObject());
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        InvocationContext context,
        IApisClient client,
        string workspaceId,
        CancellationToken ct)
    {
        var cursor = context.ParseResult.GetValueForOption(Opt<CursorOption>.Instance);
        var data = await client.ListApisAsync(workspaceId, cursor, 10, ct);

        var items = data.Items
            .Select(ApiDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        context.SetResult(new PaginatedListResult<ApiDetailPrompt.ApiDetailPromptResult>(items, data.EndCursor));

        return ExitCodes.Success;
    }
}
