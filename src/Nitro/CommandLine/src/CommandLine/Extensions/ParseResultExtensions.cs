using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Services.Sessions;

// TODO: Not sure if this is the best extension point
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

    public static async Task<bool> ConfirmWhenNotForced(
        this ParseResult parseResult,
        string message,
        INitroConsole console,
        CancellationToken cancellationToken)
    {
        var forceOption = parseResult.GetValue(Opt<ForceOption>.Instance);

        // TODO: How to do this. Maybe getResult?
        // if (forceOption is not null)
        // {
        //     return true;
        // }

        return await console.ConfirmAsync(message, cancellationToken);
    }

    public static Task<string> OptionOrAskAsync(
        this ParseResult parseResult,
        string question,
        Option<string> option,
        INitroConsole console,
        CancellationToken cancellationToken)
        => OptionOrAskAsync(parseResult, question, option, defaultValue: null, console, cancellationToken);

    public static async Task<string> OptionOrAskAsync(
        this ParseResult parseResult,
        string question,
        Option<string> option,
        string? defaultValue,
        INitroConsole console,
        CancellationToken cancellationToken)
    {
        var value = parseResult.GetValue(option);

        if (value is not null)
        {
            return value;
        }

        if (!console.IsInteractive)
        {
            throw new ExitException($"Missing required option '{option.Name}'.");
        }

        var prompt = new TextPrompt<string>(question.AsQuestion());

        if (defaultValue is not null)
        {
            prompt = prompt.DefaultValue(defaultValue);
        }

        return await prompt.ShowAsync(console, cancellationToken);
    }

    public static async Task<bool> OptionOrConfirmAsync(
        this ParseResult parseResult,
        string question,
        Option<bool?> option,
        INitroConsole console,
        CancellationToken cancellationToken)
    {
        var value = parseResult.GetValue(option);

        if (value is not null)
        {
            return value.Value;
        }

        return await new ConfirmationPrompt(question.AsQuestion())
            .ShowAsync(console, cancellationToken);
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

    public static async Task<string> GetOrPromptForClientId(
        this ParseResult parseResult,
        INitroConsole console,
        IClientsClient client,
        IApisClient apisClient,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        var clientId = parseResult.GetValue(Opt<OptionalClientIdOption>.Instance);

        if (clientId is null)
        {
            var apiId = await parseResult.GetOrPromptForApiIdAsync(
                "For which API do you want to list client versions?",
                console,
                apisClient,
                sessionService,
                cancellationToken);

            var selectedClient = await SelectClientPrompt
                .New(client, apiId)
                .Title("Select a client from the list below.")
                .RenderAsync(console, cancellationToken) ?? throw ThrowHelper.NoClientSelected();

            clientId = selectedClient.Id;
        }

        return clientId;
    }
}
