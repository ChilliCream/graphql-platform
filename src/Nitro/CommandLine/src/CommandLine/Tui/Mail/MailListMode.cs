namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Which shape the mail board's list pane renders <see cref="MailState.Rows"/>
/// in: thread rollups (the default) or the flat per-message stream.
/// <see cref="MailKeyMap"/>'s Shift+V toggles between the two.
/// </summary>
internal enum MailListMode
{
    Threads,
    Flat
}
