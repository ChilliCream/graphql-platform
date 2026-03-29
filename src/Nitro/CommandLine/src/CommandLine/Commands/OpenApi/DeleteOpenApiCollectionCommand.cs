using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class DeleteOpenApiCollectionCommand : Command
{
    public DeleteOpenApiCollectionCommand(
        INitroConsole console,
        IOpenApiClient client,
        IApisClient apisClient,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("delete")
    {
        Description = "Deletes an OpenAPI collection";

        Options.Add(Opt<ForceOption>.Instance);
        Arguments.Add(Opt<OptionalIdArgument>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(
                parseResult,
                console,
                client,
                apisClient,
                sessionService,
                resultHolder,
                parseResult.GetValue(Opt<OptionalIdArgument>.Instance),
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IOpenApiClient client,
        IApisClient apisClient,
        ISessionService sessionService,
        IResultHolder resultHolder,
        string? openApiCollectionId,
        CancellationToken cancellationToken)
    {
        console.WriteLine();
        console.WriteLine("Deleting an OpenAPI collection");
        console.WriteLine();

        const string apiMessage = "For which API do you want to delete an OpenAPI collection?";
        const string openApiCollectionMessage = "Which OpenAPI collection do you want to delete?";

        if (openApiCollectionId is null)
        {
            if (!console.IsInteractive)
            {
                throw MissingRequiredOption("id");
            }

            var workspaceId = parseResult.GetWorkspaceId(sessionService);

            var selectedApi = await SelectApiPrompt
                .New(apisClient, workspaceId)
                .Title(apiMessage)
                .RenderAsync(console, cancellationToken) ?? throw NoApiSelected();

            var apiId = selectedApi.Id;

            var selectedOpenApiCollection = await SelectOpenApiCollectionPrompt
                .New(client, apiId)
                .Title(openApiCollectionMessage)
                .RenderAsync(console, cancellationToken) ?? throw NoOpenApiCollectionSelected();

            console.WriteLine("Selected OpenAPI collection: " + selectedOpenApiCollection.Name);

            openApiCollectionId = selectedOpenApiCollection.Id;
            console.OkQuestion(openApiCollectionMessage, openApiCollectionId);
        }
        else
        {
            console.OkQuestion(openApiCollectionMessage, openApiCollectionId);
        }

        var shouldDelete = await parseResult.ConfirmWhenNotForced(
            $"Do you want to delete the OpenAPI collection with the ID {openApiCollectionId}?"
                .EscapeMarkup(),
            console,
            cancellationToken);

        if (!shouldDelete)
        {
            console.OkLine("Aborted.");
            return ExitCodes.Success;
        }

        var deletedOpenApiCollection = await client.DeleteOpenApiCollectionAsync(
            openApiCollectionId,
            cancellationToken);
        console.PrintMutationErrorsAndExit(deletedOpenApiCollection.Errors);

        if (deletedOpenApiCollection.OpenApiCollection is not IOpenApiCollectionDetailPrompt_OpenApiCollection detail)
        {
            throw Exit("Could not delete the OpenAPI collection.");
        }

        console.OkLine($"OpenAPI collection {detail.Name.AsHighlight()} was deleted.");
        resultHolder.SetResult(new ObjectResult(OpenApiCollectionDetailPrompt.From(detail).ToObject()));

        return ExitCodes.Success;
    }
}
