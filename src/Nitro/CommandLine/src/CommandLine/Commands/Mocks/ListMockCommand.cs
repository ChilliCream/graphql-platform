using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client.Mocks;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Inputs;
using ChilliCream.Nitro.CommandLine.Commands.Mocks.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Mocks;

internal sealed class ListMockCommand : Command
{
    public ListMockCommand() : base("list")
    {
        Description = "List all mock schemas in an API.";

        AddOption(Opt<OptionalApiIdOption>.Instance);
        AddOption(Opt<CursorOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IMocksClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IMocksClient client,
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
        IMocksClient client,
        CancellationToken ct)
    {
        const string apiMessage = "For which API do you want to list the mock schemas?";
        var apiId = await context.GetOrPromptForApiIdAsync(apiMessage);

        var container = PaginationContainer
            .CreateConnectionData((after, first, token) =>
                client.ListMockSchemasAsync(apiId, after, first, token))
            .PageSize(10);

        var api = await PagedTable
            .From(container)
            .Title("Mock Schemas")
            .AddColumn("Id", x => x.Id)
            .AddColumn("Name", x => x.Name)
            .RenderAsync(console, ct);

        if (api is not null)
        {
            context.SetResult(MockSchemaDetailPrompt.From(api).ToObject());
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        InvocationContext context,
        IMocksClient client,
        CancellationToken ct)
    {
        var apiId = context.ParseResult.GetValueForOption(Opt<OptionalApiIdOption>.Instance);
        if (apiId is null)
        {
            throw Exit("The API ID is required in non-interactive mode.");
        }

        var cursor = context.ParseResult.GetValueForOption(Opt<CursorOption>.Instance);
        var data = await client.ListMockSchemasAsync(apiId, cursor, 10, ct);
        var items = data.Items
            .Select(MockSchemaDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        context.SetResult(
            new PaginatedListResult<MockSchemaDetailPrompt.MockSchemaDetailPromptResult>(
                items,
                data.EndCursor));

        return ExitCodes.Success;
    }
}
