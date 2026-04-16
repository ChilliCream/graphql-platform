using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class ListOpenApiCollectionCommand : Command
{
    public ListOpenApiCollectionCommand() : base("list")
    {
        Description = "List all OpenAPI collections of an API.";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<OptionalCursorOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("openapi list --api-id \"<api-id>\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IOpenApiClient>();
        var apisClient = services.GetRequiredService<IApisClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);

        if (console.IsInteractive)
        {
            return await RenderInteractiveAsync(
                parseResult,
                console,
                client,
                apisClient,
                sessionService,
                resultHolder,
                cursor,
                ct);
        }

        return await RenderNonInteractiveAsync(parseResult, client, resultHolder, cursor, ct);
    }

    private static async Task<int> RenderInteractiveAsync(
        ParseResult parseResult,
        INitroConsole console,
        IOpenApiClient client,
        IApisClient apisClient,
        ISessionService sessionService,
        IResultHolder resultHolder,
        string? cursor,
        CancellationToken ct)
    {
        var apiId = await console.GetOrPromptForApiIdAsync("For which API do you want to list the OpenAPI collections?", parseResult, apisClient, sessionService, ct);

        var container = PaginationContainer
            .CreateConnectionData(async (after, first, token) =>
                await client.ListOpenApiCollectionsAsync(apiId, after ?? cursor, first, token)
                    ?? throw new ExitException("The API was not found."))
            .PageSize(10);

        var api = await PagedTable
            .From(container)
            .Title("OpenAPI collections of API")
            .AddColumn("Id", x => x.Id)
            .AddColumn("Name", x => x.Name)
            .RenderAsync(console, ct);

        if (api is not null)
        {
            resultHolder.SetResult(new ObjectResult(OpenApiCollectionDetailPrompt.From(api).ToObject()));
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        ParseResult parseResult,
        IOpenApiClient client,
        IResultHolder resultHolder,
        string? cursor,
        CancellationToken ct)
    {
        var apiId = parseResult.GetRequiredOptionalValue(Opt<OptionalApiIdOption>.Instance);

        var data = await client.ListOpenApiCollectionsAsync(apiId, cursor, 10, ct)
            ?? throw new ExitException("The API was not found.");
        var items = data.Items
            .Select(OpenApiCollectionDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        resultHolder.SetResult(
            new PaginatedListResult<OpenApiCollectionDetailPrompt.OpenApiCollectionDetailPromptResult>(items, data.EndCursor));

        return ExitCodes.Success;
    }
}
