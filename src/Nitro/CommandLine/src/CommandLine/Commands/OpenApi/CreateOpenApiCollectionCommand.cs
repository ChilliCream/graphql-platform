using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Components;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class CreateOpenApiCollectionCommand : Command
{
    public CreateOpenApiCollectionCommand(
        INitroConsole console,
        IOpenApiClient client,
        IApisClient apisClient,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("create")
    {
        Description = "Creates a new OpenAPI collection";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<OpenApiCollectionNameOption>.Instance);

        SetAction(async (parseResult, cancellationToken)
            => await ExecuteAsync(
                parseResult,
                console,
                client,
                apisClient,
                sessionService,
                resultHolder,
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IOpenApiClient client,
        IApisClient apisClient,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        console.WriteLine();
        console.WriteLine("Creating an OpenAPI collection");
        console.WriteLine();

        const string apiMessage = "For which API do you want to create an OpenAPI collection?";
        var apiId = await parseResult.GetOrPromptForApiIdAsync(
            apiMessage,
            console,
            apisClient,
            sessionService,
            cancellationToken);

        var name = await parseResult
            .OptionOrAskAsync("Name", Opt<OpenApiCollectionNameOption>.Instance, console, cancellationToken);

        var result = await client.CreateOpenApiCollectionAsync(
            apiId,
            name,
            cancellationToken);

        console.PrintMutationErrorsAndExit(result.Errors);

        if (result.OpenApiCollection is not {} openApiCollection)
        {
            throw Exit("Could not create OpenAPI collection.");
        }

        console.OkLine($"OpenAPI collection {openApiCollection.Name.AsHighlight()} created.");
        resultHolder.SetResult(new ObjectResult(OpenApiCollectionDetailPrompt.From(openApiCollection).ToObject()));

        return ExitCodes.Success;
    }
}
