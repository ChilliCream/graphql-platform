using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis;

internal sealed class ListApiCommand : Command
{
    public ListApiCommand(
        INitroConsole console,
        IApisClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("list")
    {
        Description = "Lists all APIs of a workspace";

        Options.Add(Opt<OptionalCursorOption>.Instance);
        Options.Add(Opt<OptionalWorkspaceIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, sessionService, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IApisClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        parseResult.AssertHasAuthentication(sessionService);

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
        IApisClient client,
        IResultHolder resultHolder,
        string workspaceId,
        CancellationToken ct)
    {
        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);
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
            resultHolder.SetResult(new ObjectResult(ApiDetailPrompt.From(api).ToObject()));
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        ParseResult parseResult,
        IApisClient client,
        IResultHolder resultHolder,
        string workspaceId,
        CancellationToken ct)
    {
        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);
        var data = await client.ListApisAsync(workspaceId, cursor, 10, ct);

        var items = data.Items
            .Select(ApiDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        resultHolder.SetResult(new PaginatedListResult<ApiDetailPrompt.ApiDetailPromptResult>(items, data.EndCursor));

        return ExitCodes.Success;
    }
}
