namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// What the mail board's detail pane currently renders: the selected
/// message alone, or its whole thread in chronological order.
/// </summary>
internal enum MailViewMode
{
    Message,
    Thread
}
