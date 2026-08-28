namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

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
