using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;

namespace ChilliCream.Nitro.CommandLine.Cloud;

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
