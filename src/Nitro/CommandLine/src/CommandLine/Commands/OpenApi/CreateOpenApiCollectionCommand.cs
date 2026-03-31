using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Components;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Options;
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
        IApisClient apisClient,
        IOpenApiClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("create")
    {
        Description = "Creates a new OpenAPI collection";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<OpenApiCollectionNameOption>.Instance);

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

        const string apiMessage = "For which API do you want to create an OpenAPI collection?";
        var apiId = await console.GetOrPromptForApiIdAsync(apiMessage, parseResult, apisClient, sessionService, cancellationToken);

        var name = await console
            .PromptAsync("Name", defaultValue: null, parseResult, Opt<OpenApiCollectionNameOption>.Instance, cancellationToken);

        await using (var activity = console.StartActivity(
            $"Creating OpenAPI collection '{name.EscapeMarkup()}' for API '{apiId.EscapeMarkup()}'",
            "Failed to create the OpenAPI collection."))
        {
            var data = await client.CreateOpenApiCollectionAsync(
                apiId,
                name,
                cancellationToken);

            if (data.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in data.Errors)
                {
                    var errorMessage = error switch
                    {
                        IApiNotFoundError err => err.Message,
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
                throw MutationReturnedNoData();
            }

            activity.Success($"Created OpenAPI collection '{name.EscapeMarkup()}'.");

            resultHolder.SetResult(new ObjectResult(OpenApiCollectionDetailPrompt.From(detail).ToObject()));

            return ExitCodes.Success;
        }
    }
}
