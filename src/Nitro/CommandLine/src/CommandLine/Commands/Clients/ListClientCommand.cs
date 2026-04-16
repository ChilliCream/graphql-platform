using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class ListClientCommand : Command
{
    public ListClientCommand() : base("list")
    {
        Description = "List all clients of an API.";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<OptionalCursorOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("client list --api-id \"<api-id>\"");

        Subcommands.Add(new ListClientVersionsCommand());
        Subcommands.Add(new ListClientPublishedVersionsCommand());

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IClientsClient>();
        var apisClient = services.GetRequiredService<IApisClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);
        var apiId = await console.GetOrPromptForApiIdAsync(
            "For which API do you want to list the clients?",
            parseResult,
            apisClient,
            sessionService,
            ct);

        if (console.IsInteractive)
        {
            return await RenderInteractiveAsync(console, client, resultHolder, apiId, cursor, ct);
        }

        return await RenderNonInteractiveAsync(client, resultHolder, apiId, cursor, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        INitroConsole console,
        IClientsClient client,
        IResultHolder resultHolder,
        string apiId,
        string? cursor,
        CancellationToken ct)
    {
        var container = PaginationContainer
            .CreateConnectionData(async (after, first, cancellationToken) =>
                await client.ListClientsAsync(apiId, after ?? cursor, first, cancellationToken)
                    ?? throw new ExitException("The API was not found."))
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
        IClientsClient client,
        IResultHolder resultHolder,
        string apiId,
        string? cursor,
        CancellationToken ct)
    {
        var page = await client.ListClientsAsync(apiId, cursor, 10, ct)
            ?? throw new ExitException("The API was not found.");

        var items = page.Items
            .Select(x => ClientDetailPrompt.From(x).ToObject())
            .ToArray();

        resultHolder.SetResult(new PaginatedListResult<ClientDetailPrompt.ClientDetailPromptResult>(
            items,
            page.EndCursor));

        return ExitCodes.Success;
    }
}
