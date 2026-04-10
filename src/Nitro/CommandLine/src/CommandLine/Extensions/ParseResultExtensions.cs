using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;

namespace ChilliCream.Nitro.CommandLine.Services.Sessions;

internal static class ParseResultExtensions
{
    public static void AssertHasAuthentication(
        this ParseResult parseResult,
        ISessionService sessionService)
    {
        var apiKey = parseResult.GetValue(Opt<OptionalApiKeyOption>.Instance);

        if (sessionService.Session is not null || apiKey is not null)
        {
            return;
        }

        throw new ExitException(
            "This command requires an authenticated user. "
            + $"Either specify '{OptionalApiKeyOption.OptionName}' or run 'nitro login'.");
    }

    public static string GetWorkspaceId(
        this ParseResult parseResult,
        ISessionService sessionService)
    {
        return sessionService.Session?.Workspace?.Id
            ?? parseResult.GetValue(Opt<OptionalWorkspaceIdOption>.Instance)
            ?? throw ThrowHelper.NoDefaultWorkspace();
    }

    public static async Task<string> GetOrPromptForApiIdAsync(
        this ParseResult parseResult,
        string message,
        INitroConsole console,
        IApisClient apisClient,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        var apiId = parseResult.GetValue(Opt<OptionalApiIdOption>.Instance);

        if (apiId is null)
        {
            var workspaceId = parseResult.GetWorkspaceId(sessionService);
            var selectedApi = await SelectApiPrompt
                .New(apisClient, workspaceId)
                .Title(message)
                .RenderAsync(console, cancellationToken) ?? throw ThrowHelper.NoApiSelected();
            apiId = selectedApi.Id;
        }

        console.OkQuestion(message, apiId);

        return apiId;
    }
}
