using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Commands.Launch;

internal sealed class LaunchCommand : Command
{
    public LaunchCommand() : base("launch")
    {
        Description = "Launch Nitro in your default browser";

        this.AddNitroCloudDefaultOptions();

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
