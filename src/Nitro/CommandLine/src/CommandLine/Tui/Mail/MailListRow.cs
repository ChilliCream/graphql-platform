using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// One row of <see cref="MailState.Rows"/>: either a collapsed or expanded
/// thread rollup, or a message - a flat-mode row (<see cref="MessageRow.ThreadChild"/>
/// false) or one of an expanded thread's indented children (true).
/// </summary>
internal abstract record MailListRow
{
    private MailListRow()
    {
    }

    /// <summary>
    /// A thread rollup row. <see cref="Expanded"/> mirrors whether
    /// the thread's messages follow as indented <see cref="MessageRow"/>
    /// rows immediately after this one in <see cref="MailState.Rows"/>.
    /// </summary>
    public sealed record Thread(MailThreadSummary Summary, bool Expanded) : MailListRow;

    /// <summary>
    /// A message row, indented beneath its thread when <c>ThreadChild</c> is true,
    /// or shown in the flat list otherwise.
    /// </summary>
    public sealed record MessageRow(MailMessage Message, bool ThreadChild) : MailListRow;
}
