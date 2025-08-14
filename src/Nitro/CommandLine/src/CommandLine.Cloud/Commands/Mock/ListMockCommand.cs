using System.CommandLine.Invocation;
using ChilliCream.Nitro.CLI.Client;
using ChilliCream.Nitro.CLI.Commands.Mock.Component;
using ChilliCream.Nitro.CLI.Exceptions;
using ChilliCream.Nitro.CLI.Option;
using ChilliCream.Nitro.CLI.Option.Binders;
using ChilliCream.Nitro.CLI.Results;
using static ChilliCream.Nitro.CLI.ThrowHelper;

namespace ChilliCream.Nitro.CLI.Commands.Mock;

internal sealed class ListMockCommand : Command
{
    public ListMockCommand() : base("list")
    {
        Description = "List all mock schemas in and API.";

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
        if (console.IsHumandReadable())
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
        const string apiMessage = "For which api do you want to list the mock schemas?";
        var apiId = await context.GetOrSelectApiId(apiMessage);

        var container = PaginationContainer
            .Create(
                (after, first, ct) =>
                    client.ListMockCommandQuery.ExecuteAsync(apiId, after, first, ct),
                static p => p.ApiById?.MockSchemas?.PageInfo,
                static p => p.ApiById?.MockSchemas?.Edges)
            .PageSize(10);

        var api = await PagedTable
            .From(container)
            .Title("Mock Schemas")
            .AddColumn("Id", x => x.Node.Id)
            .AddColumn("Name", x => x.Node.Name)
            .RenderAsync(console, ct);

        if (api?.Node is IMockSchemaDetailPrompt node)
        {
            context.SetResult(MockSchemaDetailPrompt.From(node).ToObject());
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
            .ListMockCommandQuery
            .ExecuteAsync(apiId, cursor, 10, ct);

        console.EnsureNoErrors(result);

        var endCursor = result.Data?.ApiById?.MockSchemas?.PageInfo.EndCursor;

        var items = result.Data?.ApiById?.MockSchemas?.Edges?.Select(x =>
                MockSchemaDetailPrompt.From(x.Node).ToObject())
            .ToArray() ?? [];

        context.SetResult(new PaginatedListResult(items, endCursor!));

        return ExitCodes.Success;
    }
}
