using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine;

internal static class NitroConsoleExtensions
{
    public static INitroConsoleActivity StartActivity(
        this INitroConsole console,
        string title,
        string failureMessage)
    {
        if (!console.IsInteractive)
        {
            return NitroConsoleActivity.Start(console, title, failureMessage);
        }

        return InteractiveNitroConsoleActivity.Start(console, title, failureMessage);
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
            throw ThrowHelper.MissingRequiredOption(option.Name);
        }

        return await console.PromptAsync(question, defaultValue, cancellationToken);
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
            throw ThrowHelper.MissingRequiredOption(option.Name);
        }

        return await console.ConfirmAsync(question, cancellationToken);
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

    public static async Task<string> GetOrPromptForApiIdAsync(
        this INitroConsole console,
        string message,
        ParseResult parseResult,
        IApisClient apisClient,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        var option = Opt<OptionalApiIdOption>.Instance;
        var apiId = parseResult.GetValue(option);

        if (!string.IsNullOrEmpty(apiId))
        {
            return apiId;
        }

        if (!console.IsInteractive)
        {
            throw ThrowHelper.MissingRequiredOption(option.Name);
        }

        var workspaceId = parseResult.GetWorkspaceId(sessionService);

        return await console.PromptForApiIdAsync(apisClient, workspaceId, message, cancellationToken);
    }

    public static async Task<string> PromptForApiIdAsync(
        this INitroConsole console,
        IApisClient apisClient,
        string workspaceId,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = SelectApiPrompt.New(apisClient, workspaceId);

        if (!string.IsNullOrEmpty(title))
        {
            prompt = prompt.Title(title);
        }
        var selectedApi = await prompt.RenderAsync(console, cancellationToken);
        var apiId = selectedApi?.Id;

        if (string.IsNullOrEmpty(apiId))
        {
            throw new ExitException("You did not select an API!");
        }

        return apiId;
    }

    public static void Success(this INitroConsole console, string message)
    {
        console.MarkupLine($"[green]{message}[/]");
    }

    public static void OkLine(this INitroConsole console, string message)
    {
        console.MarkupLine(Glyphs.Check.Space() + message);
    }

    public static void OkQuestion(this INitroConsole console, string question, string result)
    {
        console.MarkupLine(
            $"{Glyphs.QuestionMark.Space()}[bold]{question}[/]: [darkseagreen4]{result}[/]");
    }

    public static async Task<string> AskAsync(
        this INitroConsole console,
        string question,
        string defaultValue,
        CancellationToken cancellationToken)
    {
        var questionText = $"{question}".AsQuestion();
        var prompt = new TextPrompt<string>(questionText).DefaultValue(defaultValue);
        return await prompt.ShowAsync(console, cancellationToken);
    }
}
