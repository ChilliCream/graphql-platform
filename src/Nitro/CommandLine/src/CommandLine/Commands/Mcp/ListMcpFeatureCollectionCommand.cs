using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class ListMcpFeatureCollectionCommand : Command
{
    public ListMcpFeatureCollectionCommand(
        INitroConsole console,
        IApisClient apisClient,
        IMcpClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("list")
    {
        Description = "List all MCP feature collections of an API.";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<OptionalCursorOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, apisClient, client, sessionService, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IApisClient apisClient,
        IMcpClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        parseResult.AssertHasAuthentication(sessionService);

        if (console.IsInteractive)
        {
            return await RenderInteractiveAsync(parseResult, console, apisClient, client, sessionService, resultHolder, ct);
        }

        return await RenderNonInteractiveAsync(parseResult, client, resultHolder, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        ParseResult parseResult,
        INitroConsole console,
        IApisClient apisClient,
        IMcpClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        const string apiMessage = "For which API do you want to list the MCP Feature Collections?";
        var apiId = await console.GetOrPromptForApiIdAsync(apiMessage, parseResult, apisClient, sessionService, ct);
        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);

        var container = PaginationContainer
            .CreateConnectionData(async (after, first, token) =>
                await client.ListMcpFeatureCollectionsAsync(apiId, after ?? cursor, first, token)
                    ?? throw ThrowHelper.ThereWasAnIssueWithTheRequest("The API was not found."))
            .PageSize(10);

        var api = await PagedTable
            .From(container)
            .Title("MCP Feature Collections of API")
            .AddColumn("Id", x => x.Id)
            .AddColumn("Name", x => x.Name)
            .RenderAsync(console, ct);

        if (api is not null)
        {
            resultHolder.SetResult(new ObjectResult(McpFeatureCollectionDetailPrompt.From(api).ToObject()));
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        ParseResult parseResult,
        IMcpClient client,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        var apiId = parseResult.GetValue(Opt<OptionalApiIdOption>.Instance);
        if (apiId is null)
        {
            throw MissingRequiredOption(ApiIdOption.OptionName);
        }

        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);
        var data = await client.ListMcpFeatureCollectionsAsync(apiId, cursor, 10, ct)
            ?? throw ThrowHelper.ThereWasAnIssueWithTheRequest("The API was not found.");
        var items = data.Items
            .Select(McpFeatureCollectionDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        resultHolder.SetResult(
            new ObjectResult(new PaginatedListResult<McpFeatureCollectionDetailPrompt.McpFeatureCollectionDetailPromptResult>(items, data.EndCursor)));

        return ExitCodes.Success;
    }
}
