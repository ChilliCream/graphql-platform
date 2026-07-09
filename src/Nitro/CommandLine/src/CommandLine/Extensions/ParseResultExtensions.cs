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
            + $"Either specify '{OptionalApiKeyOption.OptionName}' or run `nitro login`.");
    }

    public static string GetWorkspaceId(
        this ParseResult parseResult,
        ISessionService sessionService)
    {
        return sessionService.Session?.Workspace?.Id
            ?? parseResult.GetValue(Opt<OptionalWorkspaceIdOption>.Instance)
            ?? throw new ExitException($"Could not determine workspace. Either login via `nitro login` or specify the '{OptionalWorkspaceIdOption.OptionName}' option.");
    }

    public static T? GetRequiredValueIfNotInteractive<T>(
        this ParseResult parseResult,
        Option<T> option,
        INitroConsole console)
    {
        var value = parseResult.GetValue(option);

        if (value is null && !console.IsInteractive)
        {
            throw ThrowHelper.MissingRequiredOption(option.Name);
        }

        return value;
    }

    public static T? GetRequiredValueIfNotInteractive<T>(
        this ParseResult parseResult,
        Argument<T> argument,
        INitroConsole console)
    {
        var value = parseResult.GetValue(argument);

        if (value is null && !console.IsInteractive)
        {
            throw ThrowHelper.MissingRequiredArgument(argument.Name);
        }

        return value;
    }

    public static T GetRequiredOptionalValue<T>(
        this ParseResult parseResult,
        Option<T> option)
    {
        var value = parseResult.GetValue(option);

        if (value is null)
        {
            throw ThrowHelper.MissingRequiredOption(option.Name);
        }

        return value;
    }
}
