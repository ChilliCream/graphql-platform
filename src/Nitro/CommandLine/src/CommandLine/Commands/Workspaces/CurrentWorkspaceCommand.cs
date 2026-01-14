using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Workspaces;

internal sealed class CurrentWorkspaceCommand : Command
{
    public CurrentWorkspaceCommand() : base("current")
    {
        Description = "Shows the name of the currently selected workspace.";

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static Task<int> ExecuteAsync(
        IAnsiConsole console,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        if (sessionService.Session?.Workspace?.Name is { } name)
        {
            console.OkLine($"Currently is {name.AsHighlight()} selected");
            return Task.FromResult(ExitCodes.Success);
        }

        console.ErrorLine(
            $"No workspace selected. Run 'nitro workspace {SetDefaultWorkspaceCommand.Command}' to set a default.");

        return Task.FromResult(ExitCodes.Error);
    }
}
