using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.OpenApi;

internal sealed class DeleteOpenApiCollectionCommand : Command
{
    public DeleteOpenApiCollectionCommand() : base("delete")
    {
        Description = "Delete an OpenAPI collection.";

        Arguments.Add(Opt<OptionalIdArgument>.Instance);
        Options.Add(Opt<OptionalForceOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("openapi delete \"<openapi-collection-id>\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var apisClient = services.GetRequiredService<IApisClient>();
        var client = services.GetRequiredService<IOpenApiClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var openApiCollectionId = parseResult.GetRequiredValueIfNotInteractive(Opt<OptionalIdArgument>.Instance, console);

        if (openApiCollectionId is null)
        {
            var workspaceId = parseResult.GetWorkspaceId(sessionService);
            var apiId = await console.PromptForApiIdAsync(
                apisClient,
                workspaceId,
                "For which API do you want to delete an OpenAPI collection?",
                cancellationToken);

            var selectedOpenApiCollection = await SelectOpenApiCollectionPrompt
                .New(client, apiId)
                .Title( "Which OpenAPI collection do you want to delete?")
                .RenderAsync(console, cancellationToken) ?? throw new ExitException("You did not select an OpenAPI collection!");

            openApiCollectionId = selectedOpenApiCollection.Id;
        }

        var force = parseResult.GetValue(Opt<OptionalForceOption>.Instance);
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

        await using (var activity = console.StartActivity(
            $"Deleting OpenAPI collection '{openApiCollectionId.EscapeMarkup()}'",
            "Failed to delete the OpenAPI collection."))
        {
            var data = await client.DeleteOpenApiCollectionAsync(
                openApiCollectionId,
                cancellationToken);

            if (data.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in data.Errors)
                {
                    var errorMessage = error switch
                    {
                        IOpenApiCollectionNotFoundError err => err.Message,
                        IUnauthorizedOperation err => err.Message,
                        IError err => Messages.UnexpectedMutationError(err),
                        _ => Messages.UnexpectedMutationError()
                    };

                    console.Error.WriteErrorLine(errorMessage);
                    return ExitCodes.Error;
                }
            }

            if (data.OpenApiCollection is not IOpenApiCollectionDetailPrompt_OpenApiCollection detail)
            {
                throw MutationReturnedNoData();
            }

            activity.Success($"Deleted OpenAPI collection '{openApiCollectionId.EscapeMarkup()}'.");

            resultHolder.SetResult(new ObjectResult(OpenApiCollectionDetailPrompt.From(detail).ToObject()));

            return ExitCodes.Success;
        }
    }
}
