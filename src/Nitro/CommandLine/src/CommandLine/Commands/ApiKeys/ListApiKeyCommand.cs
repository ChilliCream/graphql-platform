using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.CommandLine.Commands.ApiKeys.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.ApiKeys;

internal sealed class ListApiKeyCommand : Command
{
    public ListApiKeyCommand() : base("list")
    {
        Description = "List all API keys of a workspace.";

        Options.Add(Opt<OptionalCursorOption>.Instance);
        Options.Add(Opt<OptionalWorkspaceIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("api-key list");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IApiKeysClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var workspaceId = parseResult.GetWorkspaceId(sessionService);

        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);

        if (console.IsInteractive)
        {
            return await RenderInteractiveAsync(cursor, console, client, resultHolder, workspaceId, ct);
        }

        return await RenderNonInteractiveAsync(cursor, client, resultHolder, workspaceId, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        string? cursor,
        INitroConsole console,
        IApiKeysClient client,
        IResultHolder resultHolder,
        string workspaceId,
        CancellationToken ct)
    {
        var container = PaginationContainer
            .CreateConnectionData((after, first, token)
                => client.ListApiKeysAsync(workspaceId, after ?? cursor, first, token))
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
        string? cursor,
        IApiKeysClient client,
        IResultHolder resultHolder,
        string workspaceId,
        CancellationToken ct)
    {
        var data = await client.ListApiKeysAsync(workspaceId, cursor, 10, ct);

        var items = data.Items
            .Select(ApiKeyDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        resultHolder.SetResult(
            new PaginatedListResult<ApiKeyDetailPrompt.ApiKeyDetailPromptResult>(items, data.EndCursor));

        return ExitCodes.Success;
    }
}
