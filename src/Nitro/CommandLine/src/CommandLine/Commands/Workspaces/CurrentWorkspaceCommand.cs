using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Workspaces;

internal sealed class CurrentWorkspaceCommand : Command
{
    public CurrentWorkspaceCommand(
        INitroConsole console,
        ISessionService sessionService) : base("current")
    {
        Description = "Shows the name of the currently selected workspace.";

        SetAction((_, cancellationToken)
            => ExecuteAsync(console, sessionService, cancellationToken));
    }

    private static Task<int> ExecuteAsync(
        INitroConsole console,
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
