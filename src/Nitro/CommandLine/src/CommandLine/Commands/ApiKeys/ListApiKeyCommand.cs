using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.CommandLine.Commands.ApiKeys.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.ApiKeys;

internal sealed class ListApiKeyCommand : Command
{
    public ListApiKeyCommand() : base("list")
    {
        Description = "Lists all API keys of a workspace";

        AddOption(Opt<CursorOption>.Instance);
        AddOption(Opt<WorkspaceIdOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IApiKeysClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IApiKeysClient client,
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
        IApiKeysClient client,
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
            context.SetResult(ApiKeyDetailPrompt.From(apiKey).ToObject());
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        InvocationContext context,
        IApiKeysClient client,
        string workspaceId,
        CancellationToken ct)
    {
        var cursor = context.ParseResult.GetValueForOption(Opt<CursorOption>.Instance);
        var data = await client.ListApiKeysAsync(workspaceId, cursor, 10, ct);

        var items = data.Items
            .Select(ApiKeyDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        context.SetResult(new PaginatedListResult<ApiKeyDetailPrompt.ApiKeyDetailPromptResult>(items, data.EndCursor));

        return ExitCodes.Success;
    }
}
