using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.ApiKeys.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
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
        Description = "Creates a new API key";

        AddOption(Opt<ApiKeyNameOption>.Instance);
        AddOption(Opt<OptionalApiIdOption>.Instance);
        AddOption(Opt<OptionalWorkspaceIdOption>.Instance);
        AddOption(Opt<OptionalApiKeyStageConditionOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IApisClient>(),
            Bind.FromServiceProvider<IApiKeysClient>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IApisClient apisClient,
        IApiKeysClient client,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        sessionService.AssertHasAuthentication(context);

        var name = await console
            .PromptAsync(
                "Name",
                defaultValue: null,
                context,
                Opt<ApiKeyNameOption>.Instance,
                cancellationToken);

        var stageConditionName = context.ParseResult
            .GetValueForOption(Opt<OptionalApiKeyStageConditionOption>.Instance);

        var workspaceId = context.ParseResult
            .GetValueForOption(Opt<OptionalWorkspaceIdOption>.Instance);
        var apiId = context.ParseResult.GetValueForOption(Opt<OptionalApiIdOption>.Instance);

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

            workspaceId = context.RequireWorkspaceId();

            if (choice == "Api")
            {
                apiId = await console.PromptForApiIdAsync(
                    apisClient,
                    workspaceId,
                    "For which API do you want to create an API key?",
                    cancellationToken);
            }
        }

        workspaceId ??= context.RequireWorkspaceId();

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

            context.SetResult(new CreateApiKeyResult
            {
                Secret = result.Secret,
                Details = ApiKeyDetailPrompt.From(result.Key).ToObject()
            });

            return ExitCodes.Success;
        }
    }

    public class CreateApiKeyResult
    {
        public required string Secret { get; init; }

        public required ApiKeyDetailPrompt.ApiKeyDetailPromptResult Details { get; init; }
    }
}
