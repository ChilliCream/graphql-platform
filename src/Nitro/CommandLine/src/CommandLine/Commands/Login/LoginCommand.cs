using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Login;

internal sealed class LoginCommand : Command
{
    public LoginCommand() : base("login")
    {
        Description =
            "Log in interactively through your default browser";

        Options.Add(Opt<IdentityCloudUrlOption>.Instance);
        Arguments.Add(Opt<IdentityCloudUrlArgument>.Instance);

        this.AddExamples("login");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IWorkspacesClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var clientContext = services.GetRequiredService<NitroClientContext>();

        if (!console.IsInteractive)
        {
            throw new ExitException(
                "'nitro login' requires an interactive console. "
                + $"Use '{OptionalApiKeyOption.OptionName}' to authenticate command invocations in non-interactive environments.");
        }

        var cloudUrl = parseResult.GetValue(Opt<IdentityCloudUrlOption>.Instance)!;
        var url = parseResult.GetValue(Opt<IdentityCloudUrlArgument>.Instance);

        url ??= cloudUrl;

        Session? session;
        await using (var activity = console.StartActivity("Logging in via browser", "Failed to log in."))
        {
            activity.Update($"Browser opened at {url.EscapeMarkup()}. Continue login there.");

            session = await sessionService.LoginAsync(url, cancellationToken);

            if (session is null)
            {
                throw new ExitException("There was a failure and Nitro could not log you in.");
            }

            activity.Success($"Logged in as '{session.Email.EscapeMarkup()}'.");
        }

        clientContext.Configure(session.ApiUrl, new NitroClientAccessTokenAuthorization(session.Tokens!.AccessToken));

        return await SetDefaultWorkspaceCommand
            .ExecuteAsync(forceSelection: false, console, client, sessionService, cancellationToken);
    }
}
