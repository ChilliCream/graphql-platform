using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Services.Sessions;

internal static class ParseResultExtensions
{
    public static void AssertHasAuthentication(
        this ParseResult parseResult,
        ISessionService sessionService)
    {
        var apiKey = parseResult.GetValue(Opt<ApiKeyOption>.Instance);

        if (sessionService.Session is not null || apiKey is not null)
        {
            return;
        }

        throw new ExitException(
            "This command requires an authenticated user. "
            + $"Either specify '{ApiKeyOption.OptionName}' or run 'nitro login'.");
    }

    public static string GetWorkspaceId(
        this ParseResult parseResult,
        ISessionService sessionService)
    {
        return sessionService.Session?.Workspace?.Id
            ?? parseResult.GetValue(Opt<WorkspaceIdOption>.Instance)
            ?? throw ThrowHelper.NoDefaultWorkspace();
    }
    //
    // public static async Task<bool> ConfirmWhenNotForced(
    //     this InvocationContext context,
    //     string message,
    //     CancellationToken cancellationToken)
    // {
    //     var forceOption = context.ParseResult.FindResultFor(Opt<ForceOption>.Instance);
    //     if (forceOption is not null)
    //     {
    //         return true;
    //     }
    //
    //     var console = context.BindingContext.GetRequiredService<INitroConsole>();
    //
    //     return await console.ConfirmAsync(message, cancellationToken);
    // }
}
