using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class LoginCommand : Command
{
    public LoginCommand() : base("login")
    {
        Description =
            "This command logs you in with a user account. Nitro CLI will try to launch a web browser to log you in interactively";

        AddOption(Opt<IdentityCloudUrlOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Opt<IdentityCloudUrlOption>.Instance,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        string cloudUrl,
        IAnsiConsole console,
        IApiClient client,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        var session = await console
            .DefaultStatus()
            .StartAsync(
                $"A web browser has been opened at [blue underline]{cloudUrl}[/]. Please continue the login in the web browser.",
                async _ => await sessionService.LoginAsync(cloudUrl, cancellationToken));

        if (session is null)
        {
            throw new ExitException("There was a failure and nitro could not log you in.");
        }

        console.OkLine(
            $"Logged in as [bold]{session.Email}[/] ({session.Tenant} on {session.IdentityServer})");

        return await SetDefaultWorkspaceCommand
            .ExecuteAsync(forceSelection: false, console, client, sessionService, cancellationToken);
    }
}
