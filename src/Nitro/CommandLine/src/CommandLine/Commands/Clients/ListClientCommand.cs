using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Client;
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

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        CancellationToken ct)
    {
        if (console.IsHumanReadable())
        {
            return await RenderInteractiveAsync(context, console, client, ct);
        }

        return await RenderNonInteractiveAsync(context, console, client, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        CancellationToken ct)
    {
        const string apiMessage = "For which API do you want to list the clients?";
        var apiId = await context.GetOrSelectApiId(apiMessage);

        var container = PaginationContainer
            .Create((after, first, ct) =>
                    client.ListClientCommandQuery.ExecuteAsync(apiId, after, first, ct),
                static p => (p.Node as IListClientCommandQuery_Node_Api)?.Clients?.PageInfo,
                static p => (p.Node as IListClientCommandQuery_Node_Api)?.Clients?.Edges)
            .PageSize(10);

        var api = await PagedTable
            .From(container)
            .Title("Clients of api")
            .AddColumn("Id", x => x.Node.Id)
            .AddColumn("Name", x => x.Node.Name)
            .RenderAsync(console, ct);

        if (api?.Node is IClientDetailPrompt_Client node)
        {
            context.SetResult(await ClientDetailPrompt.From(node, client).ToObject([]));
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        CancellationToken ct)
    {
        var apiId = context.ParseResult.GetValueForOption(Opt<OptionalApiIdOption>.Instance);
        if (apiId is null)
        {
            throw Exit("The api id is required in non-interactive mode.");
        }

        var cursor = context.ParseResult.GetValueForOption(Opt<CursorOption>.Instance);
        var result = await client
            .ListClientCommandQuery
            .ExecuteAsync(apiId, cursor, 10, ct);

        console.EnsureNoErrors(result);

        var endCursor = (result.Data?.Node as IListClientCommandQuery_Node_Api)?.Clients?.PageInfo
            .EndCursor;

        var taskItems = (result.Data?.Node as IListClientCommandQuery_Node_Api)?.Clients?.Edges?.Select(x =>
                ClientDetailPrompt.From(x.Node, client).ToObject([]))
            .ToArray() ?? [];
        var items = await Task.WhenAll(taskItems);

        context.SetResult(new PaginatedListResult<ClientDetailPrompt.ClientDetailPromptResult>(items, endCursor));

        return ExitCodes.Success;
    }
}
