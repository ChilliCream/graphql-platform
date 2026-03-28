using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class ListClientCommand : Command
{
    public ListClientCommand(
        ListClientVersionsCommand listClientVersionsCommand,
        ListClientPublishedVersionsCommand listClientPublishedVersionsCommand,
        INitroConsole console,
        IClientsClient client,
        IApisClient apisClient,
        ISessionService sessionService,
        IResultHolder resultHolder)
        : base("list")
    {
        Description = "Lists all clients of an API";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<CursorOption>.Instance);

        Subcommands.Add(listClientVersionsCommand);
        Subcommands.Add(listClientPublishedVersionsCommand);

        SetAction(async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, apisClient, sessionService, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IClientsClient client,
        IApisClient apisClient,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        if (console.IsInteractive)
        {
            return await RenderInteractiveAsync(parseResult, console, client, apisClient, sessionService, resultHolder, ct);
        }

        return await RenderNonInteractiveAsync(parseResult, client, resultHolder, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        ParseResult parseResult,
        INitroConsole console,
        IClientsClient client,
        IApisClient apisClient,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        const string apiMessage = "For which API do you want to list the clients?";
        var apiId = await console.GetOrPromptForApiIdAsync(apiMessage, parseResult, apisClient, sessionService, ct);
        var cursor = parseResult.GetValue(Opt<CursorOption>.Instance);

        var container = PaginationContainer
            .CreateConnectionData((after, first, cancellationToken) =>
                client.ListClientsAsync(apiId, after ?? cursor, first, cancellationToken))
            .PageSize(10);

        var selectedClient = await PagedTable
            .From(container)
            .Title("Clients of API")
            .AddColumn("Id", x => x.Id)
            .AddColumn("Name", x => x.Name)
            .RenderAsync(console, ct);

        if (selectedClient is not null)
        {
            resultHolder.SetResult(new ObjectResult(ClientDetailPrompt.From(selectedClient).ToObject()));
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        ParseResult parseResult,
        IClientsClient client,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        var apiId = parseResult.GetValue(Opt<OptionalApiIdOption>.Instance);
        if (apiId is null)
        {
            throw Exit("The API ID is required in non-interactive mode.");
        }

        var cursor = parseResult.GetValue(Opt<CursorOption>.Instance);
        var page = await client.ListClientsAsync(apiId, cursor, 10, ct);

        var items = page.Items
            .Select(x => ClientDetailPrompt.From(x).ToObject())
            .ToArray();

        resultHolder.SetResult(new ObjectResult(new PaginatedListResult<ClientDetailPrompt.ClientDetailPromptResult>(
            items,
            page.EndCursor)));

        return ExitCodes.Success;
    }
}
