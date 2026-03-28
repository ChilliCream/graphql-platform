using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine;

internal static class NitroConsoleExtensions
{
    public static INitroConsoleActivity StartActivity(this INitroConsole console, string title)
    {
        return new InteractiveNitroConsoleActivity();
    }

    public static async Task<string> PromptAsync(
        this INitroConsole console,
        string question,
        string? defaultValue,
        InvocationContext context,
        Option<string> option,
        CancellationToken cancellationToken)
    {
        var value = context.ParseResult.GetValueForOption(option);

        if (value is not null)
        {
            return value;
        }

        if (!console.IsInteractive)
        {
            throw new ExitException($"Missing required option '--{option.Name}'.");
        }

        return await console.PromptAsync(question, defaultValue, cancellationToken);
    }

    public static async Task<bool> ConfirmAsync(
        this INitroConsole console,
        InvocationContext context,
        Option<bool?> option,
        string question,
        CancellationToken cancellationToken)
    {
        var value = context.ParseResult.GetValueForOption(option);

        if (value is not null)
        {
            return value.Value;
        }

        if (!console.IsInteractive)
        {
            throw new ExitException($"Missing required option '--{option.Name}'.");
        }

        return await console.ConfirmAsync(question, cancellationToken);
    }

    public static async Task<string> PromptForApiAsync(
        this INitroConsole console,
        string message,
        CancellationToken cancellationToken)
    {
        return await console.PromptAsync(message, defaultValue: null, cancellationToken);
    }

    // TODO: Properly implement
    // public static async Task<string> GetOrPromptForApiIdAsync(
    //     this INitroConsole console,
    //     string message,
    //     InvocationContext context,
    //     CancellationToken cancellationToken)
    // {
    //     var apiId = context.ParseResult.GetValueForOption(Opt<OptionalApiIdOption>.Instance);
    //
    //     if (!string.IsNullOrEmpty(apiId))
    //     {
    //         return apiId;
    //     }
    //
    //     //     var client = context.BindingContext.GetRequiredService<IApisClient>();
    //     //     var workspaceId = context.RequireWorkspaceId();
    //     //     var selectedApi = await SelectApiPrompt
    //     //         .New(client, workspaceId)
    //     //         .Title(message)
    //     //         .RenderAsync(console, ct) ?? throw ThrowHelper.NoApiSelected();
    //     //     apiId = selectedApi.Id;
    //
    //     return null;
    // }
}
