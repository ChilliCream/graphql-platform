namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Which shape the mail board's list pane renders <see cref="MailState.Rows"/>
/// in: thread rollups (the default, per the epic's threaded-table ruling) or
/// the flat per-message stream the board originally shipped with. <see cref="MailKeyMap"/>'s
/// Shift+V toggles between the two; flat mode is kept, not deleted, per the
/// epic's non-goals.
/// </summary>
internal enum MailListMode
{
    Threads,
    Flat
}
