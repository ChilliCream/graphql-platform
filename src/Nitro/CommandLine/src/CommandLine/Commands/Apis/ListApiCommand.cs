using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis;

internal sealed class ListApiCommand : Command
{
    public ListApiCommand() : base("list")
    {
        Description = "List all APIs of a workspace.";

        Options.Add(Opt<OptionalCursorOption>.Instance);
        Options.Add(Opt<OptionalWorkspaceIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("api list");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IApisClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

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
        IApisClient client,
        IResultHolder resultHolder,
        string workspaceId,
        CancellationToken ct)
    {
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
        string? cursor,
        IApisClient client,
        IResultHolder resultHolder,
        string workspaceId,
        CancellationToken ct)
    {
        var data = await client.ListApisAsync(workspaceId, cursor, 10, ct);

        var items = data.Items
            .Select(ApiDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        resultHolder.SetResult(
            new ObjectResult(new PaginatedListResult<ApiDetailPrompt.ApiDetailPromptResult>(items, data.EndCursor)));

        return ExitCodes.Success;
    }
}
