using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Logout;

internal sealed class LogoutCommand : Command
{
    public LogoutCommand(
        INitroConsole console,
        ISessionService sessionService) : base("logout")
    {
        Description = "Log out and remove session information.";

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(console, sessionService, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        INitroConsole console,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        await using (var activity = console.StartActivity("Logging out", "Failed to log out."))
        {
            await sessionService.LogoutAsync(cancellationToken);

            activity.Success("Logged out. See you soon \ud83d\udc4b");
        }

        return ExitCodes.Success;
    }
}
