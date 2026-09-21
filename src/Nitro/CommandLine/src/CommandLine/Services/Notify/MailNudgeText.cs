namespace ChilliCream.Nitro.CommandLine.Services.Notify;

internal static class MailNudgeText
{
    /// <summary>
    /// Formats an unread count and the command for listing the actor's inbox.
    /// </summary>
    public static string Format(string actor, int unreadCount)
        => $"You have {unreadCount} unread nitro message{(unreadCount == 1 ? "" : "s")}. "
            + $"Run `nitro agent mail inbox --actor {actor}`.";
}
