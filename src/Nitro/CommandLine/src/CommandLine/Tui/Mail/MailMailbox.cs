namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// The message collection displayed by the mail board.
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
