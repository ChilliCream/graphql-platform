using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Copilot;

internal sealed class SessionEndHookCommand : Command
{
    public SessionEndHookCommand() : base("session-end")
    {
        Description = "Release the generated Copilot session identity.";

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var ancestorResolver = services.GetRequiredService<ICopilotAncestorSessionResolver>();
        var processInfoProvider = services.GetRequiredService<IProcessInfoProvider>();
        var instanceIdProvider = services.GetRequiredService<INitroInstanceIdProvider>();
        var globalConfigDirectoryProvider = services.GetRequiredService<IGlobalConfigDirectoryProvider>();
        var sessions = services.GetRequiredService<IAgentSessionRegistry>();
        var ancestor = ancestorResolver.Resolve();

        if (ancestor is null)
        {
            return ExitCodes.Success;
        }

        var processStart = processInfoProvider.GetStartTicks(ancestor.Pid);

        if (processStart is null)
        {
            return ExitCodes.Success;
        }

        var host = await instanceIdProvider.GetIdAsync(
            globalConfigDirectoryProvider.GetDirectory(), cancellationToken);
        await sessions.EndEphemeralCopilotAsync(host, ancestor.Pid, processStart, cancellationToken);

        return ExitCodes.Success;
    }
}
