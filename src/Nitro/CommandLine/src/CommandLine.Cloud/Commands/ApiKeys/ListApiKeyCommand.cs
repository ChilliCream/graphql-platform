using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using ChilliCream.Nitro.CommandLine.Cloud.Results;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class ListApiKeyCommand : Command
{
    public ListApiKeyCommand() : base("list")
    {
        Description = "Lists all api keys of a workspace";

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
                    client.ListApiKeyCommandQuery.ExecuteAsync(workspaceId, after, first, ct),
                static p => p.WorkspaceById?.ApiKeys?.PageInfo,
                static p => p.WorkspaceById?.ApiKeys?.Edges)
            .PageSize(10);

        var api = await PagedTable
            .From(container)
            .Title("ApiKeys")
            .AddColumn("Id", x => x.Node.Id)
            .AddColumn("Name", x => x.Node.Name)
            .RenderAsync(console, ct);

        if (api?.Node is IApiKeyDetailPrompt_ApiKey node)
        {
            context.SetResult(ApiKeyDetailPrompt.From(node).ToResult());
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
            .ListApiKeyCommandQuery
            .ExecuteAsync(workspaceId, cursor, 10, ct);

        console.EnsureNoErrors(result);

        var endCursor = result.Data?.WorkspaceById?.ApiKeys?.PageInfo.EndCursor;

        var items = result.Data?.WorkspaceById?.ApiKeys?.Edges?.Select(x =>
                new
                {
                    x.Node.Id,
                    x.Node.Name,
                    Workspace = x.Node.Workspace is { } workspace
                        ? new { workspace.Name }
                        : null
                })
            .Cast<object>()
            .ToArray() ?? [];

        context.SetResult(new PaginatedListResult(items, endCursor!));

        return ExitCodes.Success;
    }
}
