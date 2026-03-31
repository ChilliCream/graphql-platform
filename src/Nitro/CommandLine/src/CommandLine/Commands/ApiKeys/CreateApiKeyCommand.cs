using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.ApiKeys.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.ApiKeys;

internal sealed class CreateApiKeyCommand : Command
{
    public CreateApiKeyCommand() : base("create")
    {
        Description = "Create a new API key.";

        Options.Add(Opt<ApiKeyNameOption>.Instance);
        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<OptionalWorkspaceIdOption>.Instance);
        Options.Add(Opt<OptionalApiKeyStageConditionOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(async (services, parseResult, cancellationToken) =>
        {
            var console = services.GetRequiredService<INitroConsole>();
            var apisClient = services.GetRequiredService<IApisClient>();
            var apiKeysClient = services.GetRequiredService<IApiKeysClient>();
            var sessionService = services.GetRequiredService<ISessionService>();
            var resultHolder = services.GetRequiredService<IResultHolder>();
            return await ExecuteAsync(
                parseResult,
                console,
                apisClient,
                apiKeysClient,
                sessionService,
                resultHolder,
                cancellationToken);
        });
    }

    private async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IApisClient apisClient,
        IApiKeysClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var name = await console.PromptAsync(
            "Name",
            defaultValue: null,
            parseResult,
            Opt<ApiKeyNameOption>.Instance,
            cancellationToken);

        var stageConditionName = parseResult.GetValue(Opt<OptionalApiKeyStageConditionOption>.Instance);
        var workspaceId = parseResult.GetValue(Opt<OptionalWorkspaceIdOption>.Instance);
        var apiId = parseResult.GetValue(Opt<OptionalApiIdOption>.Instance);

        if (workspaceId is null && apiId is null)
        {
            if (!console.IsInteractive)
            {
                throw MissingRequiredOption(
                    $"{OptionalWorkspaceIdOption.OptionName}' or '{ApiIdOption.OptionName}");
            }

            var choice = await console.PromptAsync(
                "Do you want to create the API key scoped to an API or the whole workspace?",
                ["Api", "Workspace"],
                cancellationToken);

            workspaceId = parseResult.GetWorkspaceId(sessionService);

            if (choice == "Api")
            {
                apiId = await console.PromptForApiIdAsync(
                    apisClient,
                    workspaceId,
                    "For which API do you want to create an API key?",
                    cancellationToken);
            }
        }

        workspaceId ??= parseResult.GetWorkspaceId(sessionService);

        await using (var activity = console.StartActivity(
            $"Creating API key '{name.EscapeMarkup()}'",
            "Failed to create the API key."))
        {
            var data = await client.CreateApiKeyAsync(
                name,
                workspaceId,
                apiId,
                stageConditionName,
                cancellationToken);

            if (data.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in data.Errors)
                {
                    var errorMessage = error switch
                    {
                        IApiNotFoundError err => err.Message,
                        IWorkspaceNotFound err => err.Message,
                        IPersonalWorkspaceNotSupportedError err => err.Message,
                        IRoleNotFoundError err => err.Message,
                        IValidationError err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error"
                    };

                    console.Error.WriteErrorLine(errorMessage);

                    return ExitCodes.Error;
                }
            }

            var result = data.Result;
            if (result is null)
            {
                throw MutationReturnedNoData();
            }

            activity.Success($"Created API key '{name.EscapeMarkup()}'.");

            resultHolder.SetResult(new ObjectResult(new CreateApiKeyResult
            {
                Secret = result.Secret,
                Details = ApiKeyDetailPrompt.From(result.Key).ToObject()
            }));

            return ExitCodes.Success;
        }
    }

    public class CreateApiKeyResult
    {
        public required string Secret { get; init; }

        public required ApiKeyDetailPrompt.ApiKeyDetailPromptResult Details { get; init; }
    }
}
