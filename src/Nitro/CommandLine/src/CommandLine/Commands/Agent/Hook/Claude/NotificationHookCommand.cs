using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Claude;

/// <summary>
/// Adapts the <c>Notification</c> hook of a harness speaking Claude Code's hook
/// contract: touches the session's last-seen time on an idle-prompt notification.
/// Payload JSON on stdin, <c>{}</c> on stdout, always.
/// </summary>
internal sealed class NotificationHookCommand : Command
{
    public NotificationHookCommand(ClaudeHooksTemplate template) : base("notification")
    {
        Description = $"Adapt {AgentHarnessDisplay.Name(template.Harness)}'s Notification hook: "
            + "touch last-seen on an idle-prompt notification.";

        this.SetHookAction(
            template.Harness,
            "Notification",
            (handler, payload, ct) => handler.HandleNotificationAsync(payload, skipSessionFileLookup: false, ct));
    }
}
