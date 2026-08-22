namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// The mail board's named mailboxes: a direct-jump axis over which message
/// corpus the list pane shows, independent of <see cref="MailListFilter"/>'s
/// read-state axis within <see cref="Inbox"/>.
/// </summary>
internal enum MailMailbox
{
    /// <summary>
    /// Messages addressed to the actor, filtered by <see cref="MailListFilter"/>.
    /// </summary>
    Inbox,

    /// <summary>
    /// Messages the actor sent, regardless of recipient.
    /// </summary>
    Sent,

    /// <summary>
    /// Every message the actor sent or received.
    /// </summary>
    All,

    /// <summary>
    /// Every message in the workspace, across every agent.
    /// </summary>
    Workspace
}

/// <summary>
/// Display metadata for <see cref="MailMailbox"/>.
/// </summary>
internal static class MailMailboxExtensions
{
    /// <summary>
    /// The mailbox's display name, shown in the mail board's list header.
    /// </summary>
    public static string DisplayName(this MailMailbox mailbox) => mailbox switch
    {
        MailMailbox.Sent => "Sent",
        MailMailbox.All => "All",
        MailMailbox.Workspace => "Workspace",
        _ => "Inbox"
    };
}
