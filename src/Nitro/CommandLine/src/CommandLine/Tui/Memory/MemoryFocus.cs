namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// Which pane of the memory tab currently holds focus, mirroring
/// <c>MailFocus</c> for the mail tab's list/detail split.
/// </summary>
internal enum MemoryFocus
{
    List,
    Detail
}
