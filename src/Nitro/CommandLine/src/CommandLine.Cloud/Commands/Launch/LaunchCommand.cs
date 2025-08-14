using ChilliCream.Nitro.CLI.Helpers;
using ChilliCream.Nitro.CLI.Option.Binders;

namespace ChilliCream.Nitro.CLI;

internal sealed class LaunchCommand : Command
{
    public LaunchCommand() : base("launch")
    {
        Description = "This command launches Nitro in your default browser";

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static Task<int> ExecuteAsync(
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        SystemBrowser.Open(Constants.NitroWebUrl);
        console.OkLine($"[link={Constants.NitroWebUrl}]Nitro[/] is launched!");

        return Task.FromResult(ExitCodes.Success);
    }
}
