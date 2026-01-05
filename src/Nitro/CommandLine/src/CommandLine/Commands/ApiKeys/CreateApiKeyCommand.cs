using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.ApiKeys.Components;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Inputs;
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
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        CancellationToken cancellationToken)
    {
        console.WriteLine();
        console.WriteLine("Creating a api key...");
        console.WriteLine();

        var workspaceId = context.ParseResult
            .GetValueForOption(Opt<OptionalWorkspaceIdOption>.Instance);
        var apiId = context.ParseResult.GetValueForOption(Opt<OptionalApiIdOption>.Instance);

        if (workspaceId is null && apiId is null)
        {
            if (!console.IsHumanReadable())
            {
                throw Exit("The workspace id or api id is required in non-interactive mode.");
            }

            var choice = await new SelectionPrompt<string>()
                .Title("Do you want to create the api key scoped to an api or the whole workspace?")
                .AddChoices("Api", "Workspace")
                .ShowAsync(console, cancellationToken);

            if (choice == "Api")
            {
                apiId = await context
                    .GetOrSelectApiId("For which api do you want to create a api key?");
            }
            else
            {
                workspaceId = context.RequireWorkspaceId();
            }
        }

        // we use the signed in workspace by default if no workspace id is provided
        workspaceId ??= context.RequireWorkspaceId();

        var name = await context
            .OptionOrAskAsync("Name", Opt<ApiKeyNameOption>.Instance, cancellationToken);

        RoleAssigmentConditionInput? condition = null;

        var stageConditionName = context.ParseResult
            .GetValueForOption(Opt<OptionalApiKeyStageConditionOption>.Instance);

        if (stageConditionName is not null)
        {
            condition = new RoleAssigmentConditionInput
            {
                StageAuthorizationCondition = new RoleAssignmentStageAuthorizationConditionInput
                {
                    Name = stageConditionName
                }
            };
        }

        var input = new CreateApiKeyInput
        {
            Name = name,
            PermissionScope = apiId is not null
                ? new ApiKeyPermissionScopeInput { ApiId = apiId }
                : new ApiKeyPermissionScopeInput { WorkspaceId = workspaceId },
            WorkspaceId = workspaceId,
            RoleAssigmentCondition = condition
        };
        var result = await client.CreateApiKeyCommandMutation
            .ExecuteAsync(input, cancellationToken);

        console.EnsureNoErrors(result);

        var data = console.EnsureData(result);

        console.PrintErrorsAndExit(data.CreateApiKey.Errors);

        var changeResult = data.CreateApiKey.Result;
        if (changeResult is null)
        {
            throw Exit("Could not create api.");
        }

        console.OkLine(
            $"Secret: {changeResult.Secret.AsHighlight()} {"This secret will not be available later!".AsDescription()}");

        if (changeResult.Key is IApiKeyDetailPrompt_ApiKey detail)
        {
            context.SetResult(new CreateApiKeyResult
            {
                Secret = changeResult.Secret,
                Details = ApiKeyDetailPrompt.From(detail).ToObject()
            });
        }

        return ExitCodes.Success;
    }

    public class CreateApiKeyResult
    {
        public required string Secret { get; init; }

        public required ApiKeyDetailPrompt.ApiKeyDetailPromptResult Details { get; init; }
    }
}
