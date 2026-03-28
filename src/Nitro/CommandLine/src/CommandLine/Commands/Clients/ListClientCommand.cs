using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Inputs;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class ListClientCommand : Command
{
    public ListClientCommand() : base("list")
    {
        Description = "Lists all clients of an API";

        AddOption(Opt<OptionalApiIdOption>.Instance);
        AddOption(Opt<CursorOption>.Instance);

        AddCommand(new ListClientVersionsCommand());
        AddCommand(new ListClientPublishedVersionsCommand());

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IClientsClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IClientsClient client,
        CancellationToken ct)
    {
        if (console.IsHumanReadable())
        {
            return await RenderInteractiveAsync(context, console, client, ct);
        }

        return await RenderNonInteractiveAsync(context, client, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        InvocationContext context,
        IAnsiConsole console,
        IClientsClient client,
        CancellationToken ct)
    {
        const string apiMessage = "For which API do you want to list the clients?";
        var apiId = await context.GetOrSelectApiId(apiMessage);
        var cursor = context.ParseResult.GetValueForOption(Opt<CursorOption>.Instance);

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
            context.SetResult(ClientDetailPrompt.From(selectedClient).ToObject());
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        InvocationContext context,
        IClientsClient client,
        CancellationToken ct)
    {
        var apiId = context.ParseResult.GetValueForOption(Opt<OptionalApiIdOption>.Instance);
        if (apiId is null)
        {
            throw Exit("The API ID is required in non-interactive mode.");
        }

        var cursor = context.ParseResult.GetValueForOption(Opt<CursorOption>.Instance);
        var page = await client.ListClientsAsync(apiId, cursor, 10, ct);

        var items = page.Items
            .Select(x => ClientDetailPrompt.From(x).ToObject())
            .ToArray();

        context.SetResult(new PaginatedListResult<ClientDetailPrompt.ClientDetailPromptResult>(
            items,
            page.EndCursor));

        return ExitCodes.Success;
    }
}
