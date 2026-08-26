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
