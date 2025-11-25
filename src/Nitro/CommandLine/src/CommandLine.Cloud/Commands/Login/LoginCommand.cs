using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class LoginCommand : Command
{
    public LoginCommand() : base("login")
    {
        Description =
            "Log in interactively through your default browser";

        AddOption(Opt<IdentityCloudUrlOption>.Instance);
        AddArgument(Opt<IdentityCloudUrlArgument>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Opt<IdentityCloudUrlOption>.Instance,
            Opt<IdentityCloudUrlArgument>.Instance,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        string cloudUrl,
        string? url,
        IAnsiConsole console,
        IApiClient client,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        url ??= cloudUrl;

        var session = await console
            .DefaultStatus()
            .StartAsync(
                $"A web browser has been opened at [blue underline]{url}[/]. Please continue the login in the web browser.",
                async _ => await sessionService.LoginAsync(url, cancellationToken));

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
