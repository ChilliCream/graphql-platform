using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Components;
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
        IApisClient apisClient,
        IOpenApiClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("delete")
    {
        Description = "Deletes an OpenAPI collection";

        Options.Add(Opt<ForceOption>.Instance);
        Arguments.Add(Opt<OptionalIdArgument>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, apisClient, client, sessionService, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IApisClient apisClient,
        IOpenApiClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var openApiCollectionId = parseResult.GetValue(Opt<OptionalIdArgument>.Instance);

        if (openApiCollectionId is null)
        {
            if (!console.IsInteractive)
            {
                throw MissingRequiredOption("id");
            }

            const string apiMessage = "For which API do you want to delete an OpenAPI collection?";
            const string openApiCollectionMessage = "Which OpenAPI collection do you want to delete?";

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

            openApiCollectionId = selectedOpenApiCollection.Id;
        }

        var force = parseResult.GetValue(Opt<ForceOption>.Instance);
        if (!force)
        {
            var confirmed = await console.ConfirmAsync(
                $"Do you want to delete the OpenAPI collection with the ID {openApiCollectionId}?"
                    .EscapeMarkup(),
                cancellationToken);

            if (!confirmed)
            {
                throw Exit("The OpenAPI collection was not deleted.");
            }
        }

        await using (var activity = console.StartActivity($"Deleting OpenAPI collection '{openApiCollectionId.EscapeMarkup()}'"))
        {
            var data = await client.DeleteOpenApiCollectionAsync(
                openApiCollectionId,
                cancellationToken);

            if (data.Errors?.Count > 0)
            {
                activity.Fail("Failed to delete the OpenAPI collection.");

                foreach (var error in data.Errors)
                {
                    var errorMessage = error switch
                    {
                        IOpenApiCollectionNotFoundError err => err.Message,
                        IUnauthorizedOperation err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    console.Error.WriteErrorLine(errorMessage);
                    return ExitCodes.Error;
                }
            }

            if (data.OpenApiCollection is not IOpenApiCollectionDetailPrompt_OpenApiCollection detail)
            {
                activity.Fail("Failed to delete the OpenAPI collection.");
                throw MutationReturnedNoData();
            }

            activity.Success($"Deleted OpenAPI collection '{openApiCollectionId.EscapeMarkup()}'.");

            resultHolder.SetResult(new ObjectResult(OpenApiCollectionDetailPrompt.From(detail).ToObject()));

            return ExitCodes.Success;
        }
    }
}
