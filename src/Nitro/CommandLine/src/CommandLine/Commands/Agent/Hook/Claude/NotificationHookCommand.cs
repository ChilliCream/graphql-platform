namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Claude;

/// <summary>
/// Adapts Claude Code's <c>Notification</c> hook: touches the session's last-seen
/// time on an idle-prompt notification. Payload JSON on stdin, <c>{}</c> on stdout,
/// always.
/// </summary>
internal sealed class NotificationHookCommand : Command
{
    public NotificationHookCommand() : base("notification")
    {
        Description = "Adapt Claude Code's Notification hook: touch last-seen on an idle-prompt notification.";

        this.SetHookAction(
            "Notification",
            (handler, payload, ct) => handler.HandleNotificationAsync(payload, skipSessionFileLookup: false, ct));
    }
}
