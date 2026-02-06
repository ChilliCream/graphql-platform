using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Inputs;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class ListMcpFeatureCollectionCommand : Command
{
    public ListMcpFeatureCollectionCommand() : base("list")
    {
        Description = "Lists all MCP Feature Collections of an API";

        AddOption(Opt<OptionalApiIdOption>.Instance);
        AddOption(Opt<CursorOption>.Instance);

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
        if (console.IsHumanReadable())
        {
            return await RenderInteractiveAsync(context, console, client, ct);
        }

        return await RenderNonInteractiveAsync(context, console, client, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        CancellationToken ct)
    {
        const string apiMessage = "For which API do you want to list the MCP Feature Collections?";
        var apiId = await context.GetOrSelectApiId(apiMessage);

        var container = PaginationContainer
            .Create((after, first, ct) =>
                    client.ListMcpFeatureCollectionCommandQuery.ExecuteAsync(apiId, after, first, ct),
                static p => (p.Node as IListMcpFeatureCollectionCommandQuery_Node_Api)?.McpFeatureCollections?.PageInfo,
                static p => (p.Node as IListMcpFeatureCollectionCommandQuery_Node_Api)?.McpFeatureCollections?.Edges)
            .PageSize(10);

        var api = await PagedTable
            .From(container)
            .Title("MCP Feature Collections of api")
            .AddColumn("Id", x => x.Node.Id)
            .AddColumn("Name", x => x.Node.Name)
            .RenderAsync(console, ct);

        if (api?.Node is IMcpFeatureCollectionDetailPrompt_McpFeatureCollection node)
        {
            context.SetResult(McpFeatureCollectionDetailPrompt.From(node).ToObject([]));
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        CancellationToken ct)
    {
        var apiId = context.ParseResult.GetValueForOption(Opt<OptionalApiIdOption>.Instance);
        if (apiId is null)
        {
            throw Exit("The api id is required in non-interactive mode.");
        }

        var cursor = context.ParseResult.GetValueForOption(Opt<CursorOption>.Instance);
        var result = await client
            .ListMcpFeatureCollectionCommandQuery
            .ExecuteAsync(apiId, cursor, 10, ct);

        console.EnsureNoErrors(result);

        var endCursor = (result.Data?.Node as IListMcpFeatureCollectionCommandQuery_Node_Api)?.McpFeatureCollections?.PageInfo
            .EndCursor;

        var items = (result.Data?.Node as IListMcpFeatureCollectionCommandQuery_Node_Api)?.McpFeatureCollections?.Edges?.Select(x =>
                McpFeatureCollectionDetailPrompt.From(x.Node).ToObject([]))
            .ToArray() ?? [];

        context.SetResult(new PaginatedListResult<McpFeatureCollectionDetailPrompt.McpFeatureCollectionDetailPromptResult>(items, endCursor));

        return ExitCodes.Success;
    }
}
