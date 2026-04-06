using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Mocks;
using ChilliCream.Nitro.CommandLine.Commands.Mocks.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Mocks;

internal sealed class ListMockCommand : Command
{
    public ListMockCommand() : base("list")
    {
        Description = "List all mock schemas in an API.";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<OptionalCursorOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("mock list --api-id \"<api-id>\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var apisClient = services.GetRequiredService<IApisClient>();
        var client = services.GetRequiredService<IMocksClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);

        if (console.IsInteractive)
        {
            return await RenderInteractiveAsync(parseResult, console, apisClient, client, sessionService, resultHolder, cursor, ct);
        }

        return await RenderNonInteractiveAsync(parseResult, client, resultHolder, cursor, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        ParseResult parseResult,
        INitroConsole console,
        IApisClient apisClient,
        IMocksClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        string? cursor,
        CancellationToken ct)
    {
        const string apiMessage = "For which API do you want to list the mock schemas?";
        var apiId = await console.GetOrPromptForApiIdAsync(apiMessage, parseResult, apisClient, sessionService, ct);

        var container = PaginationContainer
            .CreateConnectionData((after, first, token) =>
                client.ListMockSchemasAsync(apiId, after ?? cursor, first, token))
            .PageSize(10);

        var api = await PagedTable
            .From(container)
            .Title("Mock Schemas")
            .AddColumn("Id", x => x.Id)
            .AddColumn("Name", x => x.Name)
            .RenderAsync(console, ct);

        if (api is not null)
        {
            resultHolder.SetResult(new ObjectResult(MockSchemaDetailPrompt.From(api).ToObject()));
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        ParseResult parseResult,
        IMocksClient client,
        IResultHolder resultHolder,
        string? cursor,
        CancellationToken ct)
    {
        var apiId = parseResult.GetValue(Opt<OptionalApiIdOption>.Instance);
        if (apiId is null)
        {
            throw MissingRequiredOption(ApiIdOption.OptionName);
        }

        var data = await client.ListMockSchemasAsync(apiId, cursor, 10, ct);
        var items = data.Items
            .Select(MockSchemaDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        resultHolder.SetResult(
            new PaginatedListResult<MockSchemaDetailPrompt.MockSchemaDetailPromptResult>(items, data.EndCursor));

        return ExitCodes.Success;
    }
}
