using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.CommandLine.Commands.ApiKeys.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.ApiKeys;

internal sealed class ListApiKeyCommand : Command
{
    public ListApiKeyCommand(
        INitroConsole console,
        IApiKeysClient apiKeysClient,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("list")
    {
        Description = "Lists all API keys of a workspace";

        Options.Add(Opt<CursorOption>.Instance);
        Options.Add(Opt<WorkspaceIdOption>.Instance);

        SetAction(async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, apiKeysClient, sessionService, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IApiKeysClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        var workspaceId = parseResult.GetWorkspaceId(sessionService);

        if (console.IsInteractive)
        {
            return await RenderInteractiveAsync(parseResult, console, client, resultHolder, workspaceId, ct);
        }

        return await RenderNonInteractiveAsync(parseResult, client, resultHolder, workspaceId, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        ParseResult parseResult,
        INitroConsole console,
        IApiKeysClient client,
        IResultHolder resultHolder,
        string workspaceId,
        CancellationToken ct)
    {
        var container = PaginationContainer
            .CreateConnectionData((after, first, token)
                => client.ListApiKeysAsync(workspaceId, after, first, token))
            .PageSize(10);

        var apiKey = await PagedTable
            .From(container)
            .Title("API Keys")
            .AddColumn("Id", x => x.Id)
            .AddColumn("Name", x => x.Name)
            .RenderAsync(console, ct);

        if (apiKey is not null)
        {
            resultHolder.SetResult(new ObjectResult(ApiKeyDetailPrompt.From(apiKey).ToObject()));
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        ParseResult parseResult,
        IApiKeysClient client,
        IResultHolder resultHolder,
        string workspaceId,
        CancellationToken ct)
    {
        var cursor = parseResult.GetValue(Opt<CursorOption>.Instance);
        var data = await client.ListApiKeysAsync(workspaceId, cursor, 10, ct);

        var items = data.Items
            .Select(ApiKeyDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        resultHolder.SetResult(new ObjectResult(
            new PaginatedListResult<ApiKeyDetailPrompt.ApiKeyDetailPromptResult>(items, data.EndCursor)));

        return ExitCodes.Success;
    }
}
