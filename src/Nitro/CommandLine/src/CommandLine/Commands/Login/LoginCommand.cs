using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
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

        this.SetHandler(
            ExecuteAsync,
            Opt<IdentityCloudUrlOption>.Instance,
            Opt<IdentityCloudUrlArgument>.Instance,
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IWorkspacesClient>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        string cloudUrl,
        string? url,
        INitroConsole console,
        IWorkspacesClient client,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        url ??= cloudUrl;

        Session? session = null;
        await using (var _ = console.StartActivity(
            $"A web browser has been opened at [blue underline]{url}[/]. Please continue the login in the web browser."))
        {
            session = await sessionService.LoginAsync(url, cancellationToken);
        }

        if (session is null)
        {
            throw new ExitException("There was a failure and Nitro could not log you in.");
        }

        console.OkLine(
            $"Logged in as [bold]{session.Email}[/] ({session.Tenant} on {session.IdentityServer})");

        return await SetDefaultWorkspaceCommand
            .ExecuteAsync(forceSelection: false, console, client, sessionService, cancellationToken);
    }
}
