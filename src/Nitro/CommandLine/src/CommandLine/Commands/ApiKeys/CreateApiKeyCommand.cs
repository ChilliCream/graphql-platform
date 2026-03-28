using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.ApiKeys.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.ApiKeys;

internal sealed class CreateApiKeyCommand : Command
{
    public CreateApiKeyCommand(
        INitroConsole console,
        IApisClient apisClient,
        IApiKeysClient apiKeysClient,
        ISessionService sessionService) : base("create")
    {
        Description = "Creates a new API key";

        Options.Add(Opt<ApiKeyNameOption>.Instance);
        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<OptionalWorkspaceIdOption>.Instance);
        Options.Add(Opt<OptionalApiKeyStageConditionOption>.Instance);

        SetAction(async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, apisClient, apiKeysClient, sessionService, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IApisClient apisClient,
        IApiKeysClient client,
        ISessionService sessionService,
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
                throw Exit(
                    $"The '{WorkspaceIdOption.OptionName}' or '{ApiIdOption.OptionName}' option is required in non-interactive mode.");
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

        await using (var _ = console.StartActivity("Creating API key..."))
        {
            var data = await client.CreateApiKeyAsync(
                name,
                workspaceId,
                apiId,
                stageConditionName,
                cancellationToken);

            console.PrintMutationErrorsAndExit(data.Errors);

            var result = data.Result;
            if (result is null)
            {
                throw Exit("Could not create API key.");
            }

            console.OkLine(
                $"Secret: {result.Secret.AsHighlight()} {"This secret will not be available later!".AsDescription()}");

            // context.SetResult(new CreateApiKeyResult
            // {
            //     Secret = result.Secret,
            //     Details = ApiKeyDetailPrompt.From(result.Key).ToObject()
            // });

            return ExitCodes.Success;
        }
    }

    public class CreateApiKeyResult
    {
        public required string Secret { get; init; }

        public required ApiKeyDetailPrompt.ApiKeyDetailPromptResult Details { get; init; }
    }
}
