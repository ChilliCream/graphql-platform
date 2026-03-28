using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine;

internal static class NitroConsoleExtensions
{
    public static INitroConsoleActivity StartActivity(this INitroConsole console, string title)
    {
        if (!console.IsInteractive)
        {
            return NitroConsoleActivity.Start(console, title);
        }

        return InteractiveNitroConsoleActivity.Start(console, title);
    }

    public static async Task<string> PromptAsync(
        this INitroConsole console,
        string question,
        string? defaultValue,
        ParseResult parseResult,
        Option<string> option,
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

        return await console.PromptAsync(question, defaultValue, cancellationToken);
    }

    public static async Task<bool> ConfirmAsync(
        this INitroConsole console,
        ParseResult parseResult,
        Option<bool?> option,
        string question,
        CancellationToken cancellationToken)
    {
        var value = parseResult.GetValue(option);

        if (value is not null)
        {
            return value.Value;
        }

        if (!console.IsInteractive)
        {
            throw new ExitException($"Missing required option '{option.Name}'.");
        }

        return await console.ConfirmAsync(question, cancellationToken);
    }

    public static async Task<string> GetOrPromptForApiIdAsync(
        this INitroConsole console,
        string message,
        ParseResult parseResult,
        IApisClient apisClient,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        var apiId = parseResult.GetValue(Opt<OptionalApiIdOption>.Instance);

        if (!string.IsNullOrEmpty(apiId))
        {
            return apiId;
        }

        var workspaceId = parseResult.GetWorkspaceId(sessionService);

        return await console.PromptForApiIdAsync(apisClient, workspaceId, message, cancellationToken);
    }

    public static async Task<string> PromptForApiIdAsync(
        this INitroConsole console,
        IApisClient apisClient,
        string workspaceId,
        string message,
        CancellationToken cancellationToken)
    {
        var selectedApi = await SelectApiPrompt
                .New(apisClient, workspaceId)
                .Title(message)
                .RenderAsync(console, cancellationToken) ??
            throw ThrowHelper.NoApiSelected();

        return selectedApi.Id;
    }

    public static async Task<string> PromptAsync(
        this INitroConsole console,
        string question,
        string? defaultValue,
        CancellationToken cancellationToken)
    {
        if (!console.IsInteractive)
        {
            throw new ExitException(
                "Attempted to prompt the user for input, but the console is running in non-interactive mode.");
        }

        var prompt = new TextPrompt<string>(question.AsQuestion());

        if (defaultValue is not null)
        {
            prompt = prompt.DefaultValue(defaultValue);
        }

        return await prompt.ShowAsync(console, cancellationToken);
    }

    public static async Task<T> PromptAsync<T>(
        this INitroConsole console,
        string question,
        T[] items,
        CancellationToken cancellationToken)
        where T : notnull
    {
        if (!console.IsInteractive)
        {
            throw new ExitException(
                "Attempted to prompt the user for a selection, but the console is running in non-interactive mode.");
        }

        var prompt = new SelectionPrompt<T>()
            .Title(question.AsQuestion())
            .AddChoices(items);

        return await prompt.ShowAsync(console, cancellationToken);
    }

    public static async Task<bool> ConfirmAsync(
        this INitroConsole console,
        string question,
        CancellationToken cancellationToken)
    {
        if (!console.IsInteractive)
        {
            throw new ExitException(
                "Attempted to prompt the user for confirmation, but the console is running in non-interactive mode.");
        }

        return await new ConfirmationPrompt(question.AsQuestion())
            .ShowAsync(console, cancellationToken);
    }
}
