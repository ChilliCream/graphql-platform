using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class LogoutCommand : Command
{
    public LogoutCommand() : base("logout")
    {
        Description = "Log out to remove access to Nitro";

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IApiClient client,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        await console
            .DefaultStatus()
            .StartAsync(
                "Logging you out",
                async _ => await sessionService.LogoutAsync(cancellationToken));

        console.OkLine("Logged you out of Nitro CLI. See you soon :waving_hand:");

        return ExitCodes.Success;
    }
}
