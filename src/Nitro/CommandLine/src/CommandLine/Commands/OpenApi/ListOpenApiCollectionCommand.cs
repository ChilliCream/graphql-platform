using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Inputs;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class ListOpenApiCollectionCommand : Command
{
    public ListOpenApiCollectionCommand() : base("list")
    {
        Description = "Lists all OpenAPI collections of an API";

        AddOption(Opt<OptionalApiIdOption>.Instance);
        AddOption(Opt<CursorOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IOpenApiClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IOpenApiClient client,
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
        IOpenApiClient client,
        CancellationToken ct)
    {
        const string apiMessage = "For which API do you want to list the OpenAPI collections?";
        var apiId = await context.GetOrSelectApiId(apiMessage);

        var container = PaginationContainer
            .CreateConnectionData((after, first, token) =>
                client.ListOpenApiCollectionsAsync(apiId, after, first, token))
            .PageSize(10);

        var api = await PagedTable
            .From(container)
            .Title("OpenAPI collections of API")
            .AddColumn("Id", x => x.Id)
            .AddColumn("Name", x => x.Name)
            .RenderAsync(console, ct);

        if (api is not null)
        {
            context.SetResult(OpenApiCollectionDetailPrompt.From(api).ToObject());
        }

        return ExitCodes.Success;
    }

    private static async Task<int> RenderNonInteractiveAsync(
        InvocationContext context,
        IOpenApiClient client,
        CancellationToken ct)
    {
        var apiId = context.ParseResult.GetValueForOption(Opt<OptionalApiIdOption>.Instance);
        if (apiId is null)
        {
            throw Exit("The API ID is required in non-interactive mode.");
        }

        var cursor = context.ParseResult.GetValueForOption(Opt<CursorOption>.Instance);
        var data = await client.ListOpenApiCollectionsAsync(apiId, cursor, 10, ct);
        var items = data.Items
            .Select(OpenApiCollectionDetailPrompt.From)
            .Select(x => x.ToObject())
            .ToArray();

        context.SetResult(
            new PaginatedListResult<OpenApiCollectionDetailPrompt.OpenApiCollectionDetailPromptResult>(
                items,
                data.EndCursor));

        return ExitCodes.Success;
    }
}
