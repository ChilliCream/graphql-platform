namespace ChilliCream.Nitro.CommandLine.Services.Notify;

internal static class MailNudgeText
{
    /// <summary>
    /// The nudge an agent receives: how much is waiting and the command that
    /// reads it. The mail itself stays in the inbox.
    /// </summary>
    public static string Format(string actor, int unreadCount)
        => $"You have {unreadCount} unread nitro message{(unreadCount == 1 ? "" : "s")}. "
            + $"Run `nitro agent mail inbox --actor {actor}`.";
}
