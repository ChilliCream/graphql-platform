using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class ListClientPublishedVersionsCommand : Command
{
    public ListClientPublishedVersionsCommand() : base("published-versions")
    {
        Description = "Lists all published versions of a client";

        AddOption(Opt<OptionalClientIdOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IClientsClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IClientsClient client,
        CancellationToken ct)
    {
        if (console.IsInteractive())
        {
            return await RenderInteractiveAsync(context, console, client, ct);
        }

        return await RenderNonInteractiveAsync(context, client, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        InvocationContext context,
        INitroConsole console,
        IClientsClient client,
        CancellationToken ct)
    {
        var clientId = await context.GetOrSelectClientId(console, client, ct);
        var cursor = context.ParseResult.GetValueForOption(Opt<CursorOption>.Instance);

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
            context.SetResult(selectedVersion);
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        InvocationContext context,
        IClientsClient client,
        CancellationToken ct)
    {
        var clientId = context.ParseResult.GetValueForOption(Opt<OptionalClientIdOption>.Instance);
        if (clientId is null)
        {
            throw Exit("The client ID is required in non-interactive mode.");
        }

        var cursor = context.ParseResult.GetValueForOption(Opt<CursorOption>.Instance);
        var page = await client.ListClientVersionsAsync(clientId, cursor, 10, ct);

        var items = page.Items
            .Select(ToResult)
            .Where(v => v.Stages.Count > 0)
            .ToArray();

        context.SetResult(new PaginatedListResult<ClientPublishedVersionResult>(
            items, page.EndCursor));

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
