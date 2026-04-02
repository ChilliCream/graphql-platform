using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Status;

internal sealed class StatusCommand : Command
{
    public StatusCommand() : base("status")
    {
        Description = "Display the current session status.";

        this.AddExamples("status");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var sessionService = services.GetRequiredService<ISessionService>();

        var session = sessionService.Session;

        if (session is null)
        {
            throw new ExitException(
                "Not logged in. Run 'nitro login' first.");
        }

        var defaultApiUrl = Constants.ApiUrl["https://".Length..];
        var isCustomApiUrl = session.ApiUrl != defaultApiUrl
            && session.ApiUrl != Constants.ApiUrl;

        var message = $"Logged in as [green]{session.Email.EscapeMarkup()}[/]";

        if (isCustomApiUrl)
        {
            message += $" on [green]{session.ApiUrl.EscapeMarkup()}[/]";
        }

        if (session.Workspace is { } workspace)
        {
            message += $" ([green]{workspace.Name.EscapeMarkup()}[/] workspace)";
        }

        console.MarkupLine(message);

        return Task.FromResult(ExitCodes.Success);
    }
}
