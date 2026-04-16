using ChilliCream.Nitro.CommandLine.Helpers;

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
            + $"Either specify '{OptionalApiKeyOption.OptionName}' or run {"nitro login".AsCommand()}.");
    }

    public static string GetWorkspaceId(
        this ParseResult parseResult,
        ISessionService sessionService)
    {
        return sessionService.Session?.Workspace?.Id
            ?? parseResult.GetValue(Opt<OptionalWorkspaceIdOption>.Instance)
            ?? throw new ExitException($"Could not determine workspace. Either login via {"nitro login".AsCommand()} or specify the '{OptionalWorkspaceIdOption.OptionName}' option.");
    }
}
