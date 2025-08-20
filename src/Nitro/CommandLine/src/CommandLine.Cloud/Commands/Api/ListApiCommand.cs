using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using ChilliCream.Nitro.CommandLine.Cloud.Results;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class ListApiCommand : Command
{
    public ListApiCommand() : base("list")
    {
        Description = "Lists all apis of a workspace";

        AddOption(Opt<CursorOption>.Instance);
        AddOption(Opt<WorkspaceIdOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        CancellationToken ct)
    {
        var workspaceId = context.RequireWorkspaceId();

        if (console.IsHumandReadable())
        {
            return await RenderInteractiveAsync(context, console, client, workspaceId, ct);
        }

        return await RenderNonInteractiveAsync(context, console, client, workspaceId, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        string workspaceId,
        CancellationToken ct)
    {
        var container = PaginationContainer
            .Create((after, first, _) =>
                    client.ListApiCommandQuery.ExecuteAsync(workspaceId, after, first, ct),
                static p => p.WorkspaceById?.Apis?.PageInfo,
                static p => p.WorkspaceById?.Apis?.Edges)
            .PageSize(10);

        var api = await PagedTable
            .From(container)
            .Title("Apis")
            .AddColumn("Id", x => x.Node.Id)
            .AddColumn("Name", x => x.Node.Name)
            .AddColumn("Path", x => string.Join("/", x.Node.Path))
            .RenderAsync(console, ct);

        if (api?.Node is IApiDetailPrompt_Api node)
        {
            context.SetResult(ApiDetailPrompt.From(node).ToObject());
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        string workspaceId,
        CancellationToken ct)
    {
        var cursor = context.ParseResult.GetValueForOption(Opt<CursorOption>.Instance);
        var result = await client
            .ListApiCommandQuery
            .ExecuteAsync(workspaceId, cursor, 10, ct);

        console.EnsureNoErrors(result);

        var endCursor = result.Data?.WorkspaceById?.Apis?.PageInfo.EndCursor;

        var items = result.Data?.WorkspaceById?.Apis?.Edges?.Select(x =>
                ApiDetailPrompt.From(x.Node).ToObject())
            .ToArray() ?? [];

        context.SetResult(new PaginatedListResult<ApiDetailPrompt.ApiDetailPromptResult>(items, endCursor));

        return ExitCodes.Success;
    }
}
