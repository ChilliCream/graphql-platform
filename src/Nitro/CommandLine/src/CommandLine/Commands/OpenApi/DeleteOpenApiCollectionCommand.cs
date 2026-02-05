using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.CommandLine.Client;
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
   public DeleteOpenApiCollectionCommand() : base("delete")
    {
        Description = "Deletes an OpenAPI collection";

        AddOption(Opt<ForceOption>.Instance);
        AddArgument(Opt<OptionalIdArgument>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<OptionalIdArgument>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        string? openApiCollectionId,
        CancellationToken cancellationToken)
    {
        console.WriteLine();
        console.WriteLine("Deleting an OpenAPI collection");
        console.WriteLine();

        const string apiMessage = "For which api do you want to delete an OpenAPI collection?";
        const string openApiCollectionMessage = "Which OpenAPI collection do you want to delete?";

        if (openApiCollectionId is null)
        {
            if (!console.IsHumanReadable())
            {
                throw Exit("The OpenAPI collection id is required in non-interactive mode.");
            }

            var workspaceId = context.RequireWorkspaceId();

            var selectedApi = await SelectApiPrompt
                .New(client, workspaceId)
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

        var shouldDelete = await context.ConfirmWhenNotForced(
            $"Do you want to delete the OpenAPI collection with the id {openApiCollectionId}?"
                .EscapeMarkup(),
            cancellationToken);

        if (!shouldDelete)
        {
            console.OkLine("Aborted.");
            return ExitCodes.Success;
        }

        var input = new DeleteOpenApiCollectionByIdInput { OpenApiCollectionId = openApiCollectionId };
        var result =
            await client.DeleteOpenApiCollectionByIdCommandMutation.ExecuteAsync(input, cancellationToken);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.DeleteOpenApiCollectionById.Errors);

        var deletedOpenApiCollection = data.DeleteOpenApiCollectionById.OpenApiCollection;
        if (deletedOpenApiCollection is null)
        {
            throw Exit("Could not delete the OpenAPI collection.");
        }

        console.OkLine($"OpenAPI collection {deletedOpenApiCollection.Name.AsHighlight()} was deleted.");

        if (deletedOpenApiCollection is IOpenApiCollectionDetailPrompt_OpenApiCollection detail)
        {
            context.SetResult(OpenApiCollectionDetailPrompt.From(detail).ToObject([]));
        }

        return ExitCodes.Success;
    }
}
