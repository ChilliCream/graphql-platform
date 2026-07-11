using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Logout;

internal sealed class LogoutCommand : Command
{
    public LogoutCommand() : base("logout")
    {
        Description = "Log out and remove session information.";

        this.AddExamples("logout");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var sessionService = services.GetRequiredService<ISessionService>();

        await using var activity = console.StartActivity("Logging out", "Failed to log out.");

        await sessionService.LogoutAsync(cancellationToken);

        activity.Success("Logged out. See you soon :waving_hand:");

        return ExitCodes.Success;
    }
}
