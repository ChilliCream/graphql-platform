using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class ListClientPublishedVersionsCommand : Command
{
    public ListClientPublishedVersionsCommand(
        INitroConsole console,
        IClientsClient clientsClient,
        IApisClient apisClient,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("published-versions")
    {
        Description = "Lists all published versions of a client";

        Options.Add(Opt<OptionalClientIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, clientsClient, apisClient, sessionService, resultHolder, cancellationToken));
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
        var clientId = parseResult.GetValue(Opt<OptionalClientIdOption>.Instance);
        if (clientId is null)
        {
            var apiId = await console.GetOrPromptForApiIdAsync(
                "For which API do you want to list client versions?",
                parseResult, apisClient, sessionService, ct);

            var selectedClient = await SelectClientPrompt
                .New(client, apiId)
                .Title("Select a client from the list below.")
                .RenderAsync(console, ct) ?? throw NoClientSelected();

            clientId = selectedClient.Id;
        }

        var cursor = parseResult.GetValue(Opt<CursorOption>.Instance);

        var container = PaginationContainer
            .CreateConnectionData(async (after, first, cancellationToken) =>
            {
                var page = await client.ListClientVersionsAsync(
                    clientId, after ?? cursor, first, cancellationToken);

                var mappedItems = page.Items
                    .Select(ToResult)
                    .Where(v => v.Stages.Count > 0)
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
            .AddColumn("Created", x => x.CreatedAt.ToString("u"))
            .AddColumn("Stages", x => string.Join(", ", x.Stages))
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
        CancellationToken ct)
    {
        var clientId = parseResult.GetValue(Opt<OptionalClientIdOption>.Instance);
        if (clientId is null)
        {
            throw Exit("The client ID is required in non-interactive mode.");
        }

        var cursor = parseResult.GetValue(Opt<CursorOption>.Instance);
        var page = await client.ListClientVersionsAsync(clientId, cursor, 10, ct);

        var items = page.Items
            .Select(ToResult)
            .Where(v => v.Stages.Count > 0)
            .ToArray();

        resultHolder.SetResult(new ObjectResult(new PaginatedListResult<ClientPublishedVersionResult>(items, page.EndCursor)));

        return ExitCodes.Success;
    }

    private static ClientPublishedVersionResult ToResult(IClientDetailPrompt_ClientVersionEdge version)
        => new()
        {
            Tag = version.Node.Tag,
            CreatedAt = version.Node.CreatedAt,
            Stages = version.Node.PublishedTo
                .Select(t => t.Stage?.Name)
                .OfType<string>()
                .ToArray()
        };

    public class ClientPublishedVersionResult
    {
        public required string Tag { get; init; }

        public required DateTimeOffset CreatedAt { get; init; }

        public required IReadOnlyList<string> Stages { get; init; }
    }
}
