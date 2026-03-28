using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Logout;

internal sealed class LogoutCommand : Command
{
    public LogoutCommand(
        INitroConsole console,
        ISessionService sessionService) : base("logout")
    {
        Description = "Log out and remove session information";

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(console, sessionService, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        INitroConsole console,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        await using (var _ = console.StartActivity("Logging you out"))
        {
            await sessionService.LogoutAsync(cancellationToken);
        }

        console.OkLine("Logged you out of Nitro CLI. See you soon :waving_hand:");

        return ExitCodes.Success;
    }
}
