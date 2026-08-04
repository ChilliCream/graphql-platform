using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class ListClientPublishedVersionsCommand : Command
{
    public ListClientPublishedVersionsCommand() : base("published-versions")
    {
        Description = "List all published versions of a client.";

        Options.Add(Opt<OptionalClientIdOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<OptionalCursorOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            client list published-versions \
              --client-id "<client-id>" \
              --stage "dev"
            """);

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

        var stage = parseResult.GetRequiredValue(Opt<StageNameOption>.Instance);
        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);

        if (console.IsInteractive)
        {
            return await RenderInteractiveAsync(parseResult, console, client, apisClient, sessionService, resultHolder, stage, cursor, ct);
        }

        return await RenderNonInteractiveAsync(parseResult, client, resultHolder, stage, cursor, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        ParseResult parseResult,
        INitroConsole console,
        IClientsClient client,
        IApisClient apisClient,
        ISessionService sessionService,
        IResultHolder resultHolder,
        string stage,
        string? cursor,
        CancellationToken ct)
    {
        var clientId = parseResult.GetValue(Opt<OptionalClientIdOption>.Instance);
        if (clientId is null)
        {
            var apiId = await console.GetOrPromptForApiIdAsync(
                Prompts.SelectApiForListClientVersions,
                parseResult, apisClient, sessionService, ct);

            var selectedClient = await SelectClientPrompt
                .New(client, apiId)
                .Title("Select a client from the list below.")
                .RenderAsync(console, ct) ?? throw NoClientSelected();

            clientId = selectedClient.Id;
        }

        var container = PaginationContainer
            .CreateConnectionData(async (after, first, cancellationToken) =>
            {
                var page = await client.ListClientPublishedVersionsAsync(
                    clientId, stage, after ?? cursor, first, cancellationToken);

                var mappedItems = page.Items
                    .Where(e => e.Node.Version is not null)
                    .Select(ToResult)
                    .ToArray();

                return new ConnectionPage<ClientPublishedVersionResult>(
                    mappedItems,
                    page.EndCursor,
                    page.HasNextPage);
            })
            .PageSize(10);

        var selectedVersion = await PagedTable
            .From(container)
            .Title("Published Versions")
            .AddColumn("Tag", x => x.Tag)
            .AddColumn("Published", x => x.PublishedAt.ToString("u"))
            .RenderAsync(console, ct);

        if (selectedVersion is not null)
        {
            resultHolder.SetResult(new ObjectResult(selectedVersion));
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        ParseResult parseResult,
        IClientsClient client,
        IResultHolder resultHolder,
        string stage,
        string? cursor,
        CancellationToken ct)
    {
        var clientId = parseResult.GetRequiredOptionalValue(Opt<OptionalClientIdOption>.Instance);

        var page = await client.ListClientPublishedVersionsAsync(clientId, stage, cursor, 10, ct);

        var items = page.Items
            .Where(e => e.Node.Version is not null)
            .Select(ToResult)
            .ToArray();

        resultHolder.SetResult(new PaginatedListResult<ClientPublishedVersionResult>(items, page.EndCursor));

        return ExitCodes.Success;
    }

    private static ClientPublishedVersionResult ToResult(IListClientPublishedVersionsCommand_PublishedClientVersionEdge edge)
        => new()
        {
            Tag = edge.Node.Version!.Tag,
            PublishedAt = edge.Node.PublishedAt
        };

    public class ClientPublishedVersionResult
    {
        public required string Tag { get; init; }

        public required DateTimeOffset PublishedAt { get; init; }
    }
}
